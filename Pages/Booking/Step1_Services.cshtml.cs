using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.Repositories;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step1_ServicesModel : PageModel
{
    private readonly IGenericRepository<Service> _serviceRepository;

    public Step1_ServicesModel(IGenericRepository<Service> serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public List<Service> Services { get; set; } = new();

    [BindProperty]
    public List<Guid> SelectedServiceIds { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Get existing session data if available
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession") ?? new BookingSessionModel();
        SelectedServiceIds = sessionModel.SelectedServiceIds;

        var allServices = await _serviceRepository.GetAllAsync();
        Services = allServices.ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Kontrol: Hiçbir şey seçilmemişse ilerlemez
        if (SelectedServiceIds == null || !SelectedServiceIds.Any())
        {
            ModelState.AddModelError("", "Lütfen en az bir hizmet seçiniz.");
            Services = (await _serviceRepository.GetAllAsync()).ToList();
            return Page();
        }

        // 2. Session Kaydı
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession") ?? new BookingSessionModel();
        sessionModel.SelectedServiceIds = SelectedServiceIds;

        // Geçiş yaparken diğer verileri temizle ki çakışmas malicious olmasın
        sessionModel.BarberId = Guid.Empty;

        HttpContext.Session.Set("BookingSession", sessionModel);

        // 3. YÖNLENDİRME (Mutlak yol kullanmak en güvenlisidir)
        return RedirectToPage("/Booking/Step4_Barber");
    }
}
