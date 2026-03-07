using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class BlacklistedModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public BlacklistedModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Customer> Customers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Kara listedekiler YADA gelmeyişi (NoShow) > 0 olanları getir ve tersten sırala (En kritikler üstte)
        Customers = await _context.Customers
            .Include(c => c.Appointments)
            .Where(c => c.IsBlacklisted || c.Appointments.Any(a => a.Status == SoftetroBarber.Enums.AppointmentStatus.NoShow))
            .OrderByDescending(c => c.IsBlacklisted)
            .ThenByDescending(c => c.Appointments.Count(a => a.Status == SoftetroBarber.Enums.AppointmentStatus.NoShow))
            .ToListAsync();
            
        return Page();
    }

    public async Task<IActionResult> OnPostToggleBlacklistAsync(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer != null)
        {
            customer.IsBlacklisted = !customer.IsBlacklisted;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Müşteri ({(customer.FullName)}) işlem başarıyla gerçekleşti.";
        }
        
        return RedirectToPage();
    }
}
