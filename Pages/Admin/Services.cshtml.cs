using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ServicesModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ServicesModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Service> ServicesList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ServicesList = await _context.Services.OrderBy(s => s.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdatePriceAsync(Guid serviceId, decimal newPrice)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service != null)
        {
            if (newPrice < 0)
            {
                TempData["ErrorMessage"] = "Fiyat negatif olamaz.";
                return RedirectToPage();
            }

            service.Price = newPrice;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"{service.Name} hizmetinin fiyatı {newPrice:C} olarak güncellendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Hizmet bulunamadı.";
        }
        
        return RedirectToPage();
    }
}
