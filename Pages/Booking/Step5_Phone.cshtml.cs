using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.Services;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step5_PhoneModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;

    public Step5_PhoneModel(ApplicationDbContext context, IWhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    [BindProperty]
    public string CustomerName { get; set; } = string.Empty;

    [BindProperty]
    public string CustomerPhone { get; set; } = string.Empty;

    public BookingSessionModel SessionData { get; set; } = new();
    public List<Service> SelectedServices { get; set; } = new();
    public string BarberName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int TotalDuration { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        
        if (sessionModel == null || !sessionModel.SelectedServiceIds.Any() || sessionModel.SelectedDate == default || sessionModel.SelectedTime == default || sessionModel.BarberId == Guid.Empty)
        {
            return RedirectToPage("Step1_Services");
        }

        SessionData = sessionModel;

        SelectedServices = await _context.Services
            .Where(s => SessionData.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();

        var barber = await _context.Barbers.FindAsync(SessionData.BarberId);
        BarberName = barber?.Name ?? "Bilinmiyor";

        TotalPrice = SelectedServices.Sum(s => s.Price);
        TotalDuration = SelectedServices.Sum(s => s.DurationInMinutes);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null) return RedirectToPage("Step1_Services");

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerPhone))
        {
            ModelState.AddModelError("", "Ad Soyad ve Telefon Numarası zorunludur.");
            await LoadSummaryDataAsync(sessionModel);
            return Page();
        }

        // 1. Blacklist Control
        bool isBlacklisted = await _context.Customers.AnyAsync(c => c.PhoneNumber == CustomerPhone && c.IsBlacklisted);
        if (isBlacklisted)
        {
            ModelState.AddModelError("", "Bu telefon numarası sistem yöneticisi tarafından engellenmiştir. Randevu alamazsınız.");
            await LoadSummaryDataAsync(sessionModel);
            return Page();
        }

        // 2. Rate Limit & Send OTP
        try
        {
            await _whatsAppService.GenerateAndSendOtpAsync(CustomerPhone);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadSummaryDataAsync(sessionModel);
            return Page();
        }

        // 3. Save details to session and redirect
        sessionModel.CustomerName = CustomerName;
        sessionModel.CustomerPhone = CustomerPhone;
        HttpContext.Session.Set("BookingSession", sessionModel);

        return RedirectToPage("Step6_Verify");
    }

    private async Task LoadSummaryDataAsync(BookingSessionModel sessionModel)
    {
        SessionData = sessionModel;
        SelectedServices = await _context.Services
            .Where(s => SessionData.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();

        var barber = await _context.Barbers.FindAsync(SessionData.BarberId);
        BarberName = barber?.Name ?? "Bilinmiyor";
        TotalPrice = SelectedServices.Sum(s => s.Price);
        TotalDuration = SelectedServices.Sum(s => s.DurationInMinutes);
    }
}
