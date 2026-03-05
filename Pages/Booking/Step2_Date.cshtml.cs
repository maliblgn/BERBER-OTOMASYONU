using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SoftetroBarber.Extensions;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step2_DateModel : PageModel
{
    [BindProperty]
    public DateTime? SelectedDate { get; set; }

    public IActionResult OnGet()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null || !sessionModel.SelectedServiceIds.Any())
        {
            return RedirectToPage("Step1_Services");
        }

        // Eğer önceden seçili tarih varsa getir, yoksa bugünün tarihi (ama geçmiş saatlere dikkat edilmeli sonraki adımda)
        SelectedDate = sessionModel.SelectedDate == default ? DateTime.Today : sessionModel.SelectedDate;
        
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!SelectedDate.HasValue || SelectedDate.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError("", "Lütfen geçerli bir tarih seçiniz.");
            return Page();
        }

        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession") ?? new BookingSessionModel();
        sessionModel.SelectedDate = SelectedDate.Value;
        HttpContext.Session.Set("BookingSession", sessionModel);

        return RedirectToPage("Step3_Time");
    }
}
