using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.Services;
using SoftetroBarber.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        if (sessionModel == null || sessionModel.BarberId == Guid.Empty)
        {
            return RedirectToPage("Step1_Services");
        }

        await LoadSummaryDataAsync(sessionModel);
        return Page();
    }

    public async Task<JsonResult> OnGetCheckCustomerAsync(string phone)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PhoneNumber == phone);

        if (customer != null)
        {
            return new JsonResult(new { found = true, name = customer.FullName });
        }
        return new JsonResult(new { found = false });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null) return RedirectToPage("Step1_Services");

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerPhone))
        {
            ModelState.AddModelError("", "Lütfen ad soyad ve telefon numaranızı girin.");
            await LoadSummaryDataAsync(sessionModel);
            return Page();
        }

        bool isBlacklisted = await _context.Customers.AnyAsync(c => c.PhoneNumber == CustomerPhone && c.IsBlacklisted);
        if (isBlacklisted)
        {
            ModelState.AddModelError("", "Bu numara ile randevu alımı engellenmiştir.");
            await LoadSummaryDataAsync(sessionModel);
            return Page();
        }

        try
        {
            await _whatsAppService.GenerateAndSendOtpAsync(CustomerPhone);

            sessionModel.CustomerName = CustomerName;
            sessionModel.CustomerPhone = CustomerPhone;
            HttpContext.Session.Set("BookingSession", sessionModel);

            return RedirectToPage("Step6_Verify");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Kod gönderilemedi: " + ex.Message);
            await LoadSummaryDataAsync(sessionModel);
            return Page();
        }
    }

    private async Task LoadSummaryDataAsync(BookingSessionModel sessionModel)
    {
        SessionData = sessionModel;
        SelectedServices = await _context.Services
            .Where(s => SessionData.SelectedServiceIds.Contains(s.Id))
            .ToListAsync();

        var barber = await _context.Barbers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == SessionData.BarberId);

        BarberName = barber?.Name ?? "Bilinmiyor";
        TotalPrice = SelectedServices.Sum(s => s.Price);
        TotalDuration = SelectedServices.Sum(s => s.DurationInMinutes);
    }
}