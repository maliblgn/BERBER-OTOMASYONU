using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.ViewModels; // Eğer bu klasör yoksa hata verebilir, o yüzden DTO'yu aşağıya gömdük
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftetroBarber.Pages.Booking;

// DİKKAT: Sınıf ismini Step2_DateTimeModel yaptık
public class Step2_DateTimeModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public Step2_DateTimeModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime SelectedDate { get; set; }

    public List<TimeSlotDto> TimeSlots { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");

        // 1. Güvenlik: Berber seçilmediyse geri gönder
        if (sessionModel == null || sessionModel.BarberId == Guid.Empty)
        {
            return RedirectToPage("/Booking/Step4_Barber");
        }

        if (SelectedDate == default) SelectedDate = DateTime.Today;

        // 2. Hizmet Sürelerini Hesapla
        var services = await _context.Services
            .Where(s => sessionModel.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();
        int totalDuration = services.Sum(s => s.DurationInMinutes);
        if (totalDuration <= 0) totalDuration = 30;

        // 3. Berber Mesaisi
        var workingHour = await _context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.BarberId == sessionModel.BarberId && wh.DayOfWeek == SelectedDate.DayOfWeek);

        if (workingHour == null)
        {
            TimeSlots = new List<TimeSlotDto>();
            return Page();
        }

        // 4. Randevu Çakışma Kontrolü (Statü Enum hatası almamak için basitleştirildi)
        var existingAppointments = await _context.Appointments
            .Where(a => a.BarberId == sessionModel.BarberId && a.StartTime.Date == SelectedDate.Date)
            .ToListAsync();

        // 5. Slot Üretimi
        TimeSlots = new List<TimeSlotDto>();
        var currentTime = workingHour.OpenTime;

        while (currentTime + TimeSpan.FromMinutes(totalDuration) <= workingHour.CloseTime)
        {
            bool isBooked = existingAppointments.Any(a =>
                (currentTime >= a.StartTime.TimeOfDay && currentTime < a.EndTime.TimeOfDay) ||
                (currentTime + TimeSpan.FromMinutes(totalDuration) > a.StartTime.TimeOfDay &&
                 currentTime + TimeSpan.FromMinutes(totalDuration) <= a.EndTime.TimeOfDay));

            if (SelectedDate.Date == DateTime.Today && currentTime < DateTime.Now.TimeOfDay)
                isBooked = true;

            TimeSlots.Add(new TimeSlotDto { Time = currentTime, IsBooked = isBooked });
            currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
        }

        return Page();
    }

    public IActionResult OnPost(TimeSpan selectedTime)
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null) return RedirectToPage("/Booking/Step1_Services");

        sessionModel.SelectedDate = SelectedDate;
        sessionModel.SelectedTime = selectedTime;
        HttpContext.Session.Set("BookingSession", sessionModel);

        return RedirectToPage("/Booking/Step5_Phone");
    }
}

// DTO sınıfını burada tutuyoruz ki CS0246 hatası gelmesin 
public class TimeSlotDto
{
    public TimeSpan Time { get; set; }
    public bool IsBooked { get; set; }
}