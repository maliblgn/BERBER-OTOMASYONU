using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step2_DateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public Step2_DateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime SelectedDate { get; set; }

    public List<TimeSlotDto> TimeSlots { get; set; } = new();

    // Arayüzdeki akıllı mesajlar için bayraklar
    public bool IsShopClosed { get; set; }
    public bool IsBarberClosed { get; set; }
    public string SelectedBarberName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // 1. Session ve Temel Kontroller
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");

        if (sessionModel == null || sessionModel.BarberId == Guid.Empty)
        {
            return RedirectToPage("/Booking/Step4_Barber");
        }

        if (SelectedDate == default) SelectedDate = DateTime.Today;

        // Berber bilgisini çek (Mesajlarda kullanmak için)
        var barber = await _context.Barbers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == sessionModel.BarberId);
        SelectedBarberName = barber?.Name ?? "Berberimiz";

        // 2. Dükkan Genel Mesaisi Kontrolü (BarberId == null olan kayıt)
        var generalHours = await _context.WorkingHours.AsNoTracking()
            .FirstOrDefaultAsync(wh => wh.BarberId == null && wh.DayOfWeek == SelectedDate.DayOfWeek);

        // Dükkan kapalıysa bayrağı dik ve çık
        if (generalHours != null && generalHours.IsClosed)
        {
            IsShopClosed = true;
            return Page();
        }

        // Dükkan genel saati yoksa varsayılan 09:00 - 22:00
        TimeSpan shopOpen = generalHours?.OpenTime ?? new TimeSpan(9, 0, 0);
        TimeSpan shopClose = generalHours?.CloseTime ?? new TimeSpan(22, 0, 0);

        // 3. Berber Özel Mesaisi Kontrolü
        var barberHours = await _context.WorkingHours.AsNoTracking()
            .FirstOrDefaultAsync(wh => wh.BarberId == sessionModel.BarberId && wh.DayOfWeek == SelectedDate.DayOfWeek);

        // Berberin kaydı yoksa veya kapalıysa bayrağı dik ve çık
        if (barberHours == null || barberHours.IsClosed)
        {
            IsBarberClosed = true;
            return Page();
        }

        // 4. Hizmet Sürelerini Hesapla
        var services = await _context.Services.AsNoTracking()
            .Where(s => sessionModel.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();
        int totalDuration = services.Sum(s => s.DurationInMinutes);
        if (totalDuration <= 0) totalDuration = 30;

        // 5. Mevcut Randevuları Çek (Çakışma kontrolü için)
        var existingAppointments = await _context.Appointments.AsNoTracking()
            .Where(a => a.BarberId == sessionModel.BarberId &&
                        a.StartTime.Date == SelectedDate.Date &&
                        a.Status != AppointmentStatus.Cancelled) // İptal edilenleri sayma
            .ToListAsync();

        // 6. Slot Üretimi
        TimeSlots = new List<TimeSlotDto>();

        // Ortak çalışma aralığını belirle
        TimeSpan startLimit = barberHours.OpenTime > shopOpen ? barberHours.OpenTime : shopOpen;
        TimeSpan endLimit = barberHours.CloseTime < shopClose ? barberHours.CloseTime : shopClose;

        var currentTime = startLimit;

        while (currentTime + TimeSpan.FromMinutes(totalDuration) <= endLimit)
        {
            // Çakışma kontrolü
            bool isBooked = existingAppointments.Any(a =>
                (currentTime >= a.StartTime.TimeOfDay && currentTime < a.EndTime.TimeOfDay) ||
                (currentTime + TimeSpan.FromMinutes(totalDuration) > a.StartTime.TimeOfDay &&
                 currentTime + TimeSpan.FromMinutes(totalDuration) <= a.EndTime.TimeOfDay));

            // Bugün bakılıyorsa geçmiş saatleri kapat
            if (SelectedDate.Date == DateTime.Today && currentTime < DateTime.Now.TimeOfDay)
                isBooked = true;

            TimeSlots.Add(new TimeSlotDto { Time = currentTime, IsBooked = isBooked });

            // 30 dakikalık periyotlarla ilerle
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

public class TimeSlotDto
{
    public TimeSpan Time { get; set; }
    public bool IsBooked { get; set; }
}