using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class BarberScheduleModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public BarberScheduleModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Barber> Barbers { get; set; } = new();
    
    public List<TimeOffs> RecentTimeOffs { get; set; } = new();

    [BindProperty]
    public Guid SelectedBarberId { get; set; }

    [BindProperty]
    public DateTime MolaDate { get; set; } = DateTime.Today;

    [BindProperty]
    public TimeSpan StartTime { get; set; } = new TimeSpan(12, 0, 0);

    [BindProperty]
    public TimeSpan EndTime { get; set; } = new TimeSpan(13, 0, 0);

    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        Barbers = await _context.Barbers.Where(b => b.IsActive).ToListAsync();
        
        RecentTimeOffs = await _context.TimeOffs
            .Include(t => t.Barber)
            .Where(t => t.StartDateTime >= DateTime.Today.AddDays(-7))
            .OrderByDescending(t => t.StartDateTime)
            .Take(20)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (SelectedBarberId == Guid.Empty)
        {
            ModelState.AddModelError("", "Lütfen bir berber seçiniz.");
            return await OnGetAsync();
        }

        if (EndTime <= StartTime)
        {
            ModelState.AddModelError("", "Bitiş saati başlangıç saatinden büyük olmalıdır.");
            return await OnGetAsync();
        }

        var timeOff = new TimeOffs
        {
            Id = Guid.NewGuid(),
            BarberId = SelectedBarberId,
            StartDateTime = MolaDate.Date + StartTime,
            EndDateTime = MolaDate.Date + EndTime,
            Reason = Reason
        };

        await _context.TimeOffs.AddAsync(timeOff);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Mola başarıyla eklendi. Müşteri takviminde artık bu saatler arası kapalı görünecek.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid timeOffId)
    {
        var timeOff = await _context.TimeOffs.FindAsync(timeOffId);
        if (timeOff != null)
        {
            _context.TimeOffs.Remove(timeOff);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
