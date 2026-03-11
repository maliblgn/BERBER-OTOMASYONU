using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;
using SoftetroBarber.Enums;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CustomersModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public CustomersModel(ApplicationDbContext context) => _context = context;

    public List<Customer> Customers { get; set; } = new();

    // Arama terimini URL'den yakalamak için
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Customers
            .Include(c => c.Appointments)
            .AsQueryable();

        // FİLTRE: İsim veya Telefon araması
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(c => c.FullName.Contains(SearchTerm) ||
                                     c.PhoneNumber.Contains(SearchTerm));
        }

        Customers = await query.OrderBy(c => c.FullName).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleBlacklistAsync(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer != null) { customer.IsBlacklisted = !customer.IsBlacklisted; await _context.SaveChangesAsync(); }
        return RedirectToPage(new { SearchTerm }); // Aramayı koruyarak dön
    }

    public async Task<IActionResult> OnPostResetLoyaltyAsync(Guid customerId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.CustomerId == customerId && a.Status == AppointmentStatus.NoShow)
            .ToListAsync();
        foreach (var apt in appointments) { apt.Status = AppointmentStatus.Cancelled; }
        await _context.SaveChangesAsync();
        return RedirectToPage(new { SearchTerm }); // Aramayı koruyarak dön
    }
}