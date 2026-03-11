using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step4_BarberModel : PageModel
{
    private readonly ApplicationDbContext _context;

    // Sadece Context'i enjekte ediyoruz, servise ihtiyacımız kalmadı
    public Step4_BarberModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Barber> AvailableBarbers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");

        // Session kontrolü
        if (sessionModel == null || sessionModel.SelectedServiceIds == null || !sessionModel.SelectedServiceIds.Any())
        {
            return RedirectToPage("/Booking/Step1_Services");
        }

        // HATA VEREN SERVİS YERİNE DOĞRUDAN SORGULUYORUZ:
        // Sadece aktif olan berberleri veritabanından çek
        AvailableBarbers = await _context.Barbers
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();

        return Page();
    }

    public IActionResult OnPost(Guid barberId)
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession") ?? new BookingSessionModel();

        if (barberId == Guid.Empty)
        {
            ModelState.AddModelError("", "Lütfen bir usta seçin.");
            return Page();
        }

        // Seçilen berberi kaydet
        sessionModel.BarberId = barberId;
        HttpContext.Session.Set("BookingSession", sessionModel);

        // BİRLEŞİK TARİH/SAAT SAYFASINA GİT
        return RedirectToPage("/Booking/Step2_Date");
    }
}