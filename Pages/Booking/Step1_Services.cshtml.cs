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
        if (SelectedServiceIds == null || !SelectedServiceIds.Any())
        {
            ModelState.AddModelError("", "Lütfen en az bir hizmet seçiniz.");
            var allServices = await _serviceRepository.GetAllAsync();
            Services = allServices.ToList();
            return Page();
        }

        // Update Session
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession") ?? new BookingSessionModel();
        sessionModel.SelectedServiceIds = SelectedServiceIds;
        HttpContext.Session.Set("BookingSession", sessionModel);

        return RedirectToPage("Step2_Date");
    }
}
