using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CustomersModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CustomersModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Customer> Customers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Customers = await _context.Customers.OrderBy(c => c.FullName).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleBlacklistAsync(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer != null)
        {
            customer.IsBlacklisted = !customer.IsBlacklisted;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Müşteri ({(customer.FullName)}) kara liste durumu güncellendi.";
        }
        
        return RedirectToPage();
    }
}
