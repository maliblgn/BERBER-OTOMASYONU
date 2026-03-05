using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.Services;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step4_BarberModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly ApplicationDbContext _context;

    public Step4_BarberModel(IBookingService bookingService, ApplicationDbContext context)
    {
        _bookingService = bookingService;
        _context = context;
    }

    public List<Barber> AvailableBarbers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        
        // Ensure previous steps are completed
        if (sessionModel == null || !sessionModel.SelectedServiceIds.Any() || sessionModel.SelectedDate == default || sessionModel.SelectedTime == default)
        {
            return RedirectToPage("Step1_Services");
        }

        // Calculate total duration
        var services = await _context.Services
            .Where(s => sessionModel.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();
            
        int totalDuration = services.Sum(s => s.DurationInMinutes);

        // Fetch barbers
        AvailableBarbers = await _bookingService.GetAvailableBarbersForSlotAsync(sessionModel.SelectedDate, sessionModel.SelectedTime, totalDuration);

        // If no barbers available, maybe someone just booked it while user was on step 3. Let them go back.
        if (!AvailableBarbers.Any())
        {
            TempData["ErrorMessage"] = "Seçtiğiniz saat az önce doldu. Lütfen başka bir saat seçiniz.";
            return RedirectToPage("Step3_Time");
        }

        return Page();
    }

    public IActionResult OnPost(Guid barberId)
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null) return RedirectToPage("Step1_Services");

        if (barberId == Guid.Empty)
        {
            ModelState.AddModelError("", "Lütfen bir berber seçiniz.");
            return Page(); // Ideally re-fetch barbers, but form won't submit empty id from buttons
        }

        sessionModel.BarberId = barberId;
        HttpContext.Session.Set("BookingSession", sessionModel);

        return RedirectToPage("Step5_Phone");
    }
}
