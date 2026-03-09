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
        // Hizmetleri isim sırasına göre listeliyoruz
        ServicesList = await _context.Services.OrderBy(s => s.Name).ToListAsync();
        return Page();
    }

    // Services.cshtml.cs
    // Services.cshtml.cs içindeki metodlar
    // Services.cshtml.cs
    public async Task<JsonResult> OnPostAutoUpdateAsync(Guid serviceId, decimal? price, int? duration)
    {
        try
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return new JsonResult(new { success = false, message = "Hizmet bulunamadı." });

            if (price.HasValue) service.Price = price.Value;
            if (duration.HasValue) service.DurationInMinutes = duration.Value;

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                message = $"{service.Name} başarıyla güncellendi."
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "Hata: " + ex.Message });
        }
    }
}