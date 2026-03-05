using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class BarbersModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public BarbersModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Barber> Barbers { get; set; } = new();

    [BindProperty]
    public string NewBarberName { get; set; } = string.Empty;

    [BindProperty]
    public string? NewBarberBio { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Barbers = await _context.Barbers.OrderByDescending(b => b.IsActive).ThenBy(b => b.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddBarberAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBarberName))
        {
            TempData["ErrorMessage"] = "Berber adı boş olamaz.";
            return RedirectToPage();
        }

        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = NewBarberName,
            Bio = NewBarberBio,
            IsActive = true
        };

        await _context.Barbers.AddAsync(barber);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{NewBarberName} isimli berber başarıyla eklendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid barberId)
    {
        var barber = await _context.Barbers.FindAsync(barberId);
        if (barber != null)
        {
            barber.IsActive = !barber.IsActive;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"{barber.Name} isimli berberin durumu güncellendi.";
        }
        
        return RedirectToPage();
    }
}
