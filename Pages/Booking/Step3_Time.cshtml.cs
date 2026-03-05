using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.Services;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step3_TimeModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly ApplicationDbContext _context;

    public Step3_TimeModel(IBookingService bookingService, ApplicationDbContext context)
    {
        _bookingService = bookingService;
        _context = context;
    }

    public DateTime SelectedDate { get; set; }
    public List<TimeSlotDto> TimeSlots { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null || !sessionModel.SelectedServiceIds.Any() || sessionModel.SelectedDate == default)
        {
            return RedirectToPage("Step1_Services");
        }

        SelectedDate = sessionModel.SelectedDate;

        // Calculate total duration
        var services = await _context.Services
            .Where(s => sessionModel.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();
            
        int totalDuration = services.Sum(s => s.DurationInMinutes);

        // Get slots
        TimeSlots = await _bookingService.GetTimeSlotsForDateAsync(SelectedDate, totalDuration);

        return Page();
    }

    public IActionResult OnPost(TimeSpan selectedTime)
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null) return RedirectToPage("Step1_Services");

        sessionModel.SelectedTime = selectedTime;
        HttpContext.Session.Set("BookingSession", sessionModel);

        return RedirectToPage("Step4_Barber");
    }
}
