using SoftetroBarber.Models;

namespace SoftetroBarber.Services;

public class TimeSlotDto
{
    public TimeSpan Time { get; set; }
    public bool IsBooked { get; set; }
}

public interface IBookingService
{
    Task<List<TimeSlotDto>> GetTimeSlotsForDateAsync(DateTime date, int totalDurationMinutes);
    
    Task<List<Barber>> GetAvailableBarbersForSlotAsync(DateTime date, TimeSpan startTime, int totalDurationMinutes);
}
