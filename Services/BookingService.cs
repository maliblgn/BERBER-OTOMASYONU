using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;
using SoftetroBarber.Repositories;

namespace SoftetroBarber.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Barber>> GetAvailableBarbersForSlotAsync(DateTime date, TimeSpan startTime, int totalDurationMinutes)
    {
        var availableBarbers = new List<Barber>();
        var requiredTimeSpan = TimeSpan.FromMinutes(totalDurationMinutes);
        var potentialStart = date.Date + startTime;
        var potentialEnd = potentialStart + requiredTimeSpan;

        // Skip if requested time is in the past
        if (potentialStart <= DateTime.Now)
            return availableBarbers;

        var activeBarbers = await _context.Barbers
            .Where(b => b.IsActive)
            .Include(b => b.WorkingHours)
            .Include(b => b.TimeOffs)
            .Include(b => b.Appointments.Where(a => a.Status != AppointmentStatus.Cancelled))
            .ToListAsync();

        foreach (var barber in activeBarbers)
        {
            // 1. Check Working Hours
            var workingHours = barber.WorkingHours.FirstOrDefault(w => w.DayOfWeek == date.DayOfWeek);
            if (workingHours == null) continue; // Not working today

            if (startTime < workingHours.OpenTime || startTime + requiredTimeSpan > workingHours.CloseTime)
                continue; // Outside working hours

            // 2. Check Time Offs
            bool hasTimeOff = barber.TimeOffs.Any(t => potentialStart < t.EndDateTime && potentialEnd > t.StartDateTime);
            if (hasTimeOff) continue;

            // 3. Check existing appointments
            bool hasConflict = barber.Appointments.Any(a => potentialStart < a.EndTime && potentialEnd > a.StartTime);
            if (hasConflict) continue;

            // Barber is available!
            availableBarbers.Add(barber);
        }

        return availableBarbers;
    }

    public async Task<List<TimeSlotDto>> GetTimeSlotsForDateAsync(DateTime date, int totalDurationMinutes)
    {
        var slots = new List<TimeSlotDto>();
        
        // Find min open time and max close time for this day of week among active barbers
        var workingHours = await _context.WorkingHours
            .Include(w => w.Barber)
            .Where(w => w.Barber.IsActive && w.DayOfWeek == date.DayOfWeek)
            .ToListAsync();

        if (!workingHours.Any()) return slots; // Global off day

        var minOpen = workingHours.Min(w => w.OpenTime);
        var maxClose = workingHours.Max(w => w.CloseTime);
        var requiredTimeSpan = TimeSpan.FromMinutes(totalDurationMinutes);
        var slotInterval = TimeSpan.FromMinutes(30); // Generate 30 mins intervals for UI

        var currentTime = minOpen;
        while (currentTime + requiredTimeSpan <= maxClose)
        {
            var potentialStart = date.Date + currentTime;
            bool isBooked = true;

            // If it's a future slot, check if any barber is available
            if (potentialStart > DateTime.Now)
            {
                var availableBarbers = await GetAvailableBarbersForSlotAsync(date, currentTime, totalDurationMinutes);
                if (availableBarbers.Any())
                {
                    isBooked = false; // At least one barber is free
                }
            }
            
            slots.Add(new TimeSlotDto
            {
                Time = currentTime,
                IsBooked = isBooked
            });

            currentTime = currentTime.Add(slotInterval);
        }

        return slots;
    }
}
