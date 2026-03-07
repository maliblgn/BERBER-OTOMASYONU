using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages;

public class OurTeamModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public OurTeamModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Barber> Barbers { get; set; } = new();

    public async Task OnGetAsync()
    {
        Barbers = await _context.Barbers
            .Where(b => b.IsActive)
            .ToListAsync();
    }
}
