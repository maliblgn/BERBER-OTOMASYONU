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
    public BarbersModel(ApplicationDbContext context) => _context = context;

    public List<Barber> Barbers { get; set; } = new();

    [BindProperty] public string NewBarberName { get; set; } = string.Empty;
    [BindProperty] public string? NewBarberBio { get; set; }

    public async Task OnGetAsync()
    {
        Barbers = await _context.Barbers.OrderByDescending(b => b.IsActive).ThenBy(b => b.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddBarberAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBarberName)) return RedirectToPage();

        var barber = new Barber { Id = Guid.NewGuid(), Name = NewBarberName, Bio = NewBarberBio, IsActive = true };

        // Varsayılan çalışma saatleri ekleme (09:00 - 19:00)
        var workingHours = Enumerable.Range(1, 6).Select(day => new WorkingHours
        {
            Id = Guid.NewGuid(),
            BarberId = barber.Id,
            DayOfWeek = (DayOfWeek)day,
            OpenTime = new TimeSpan(9, 0, 0),
            CloseTime = new TimeSpan(19, 0, 0)
        }).ToList();

        await _context.Barbers.AddAsync(barber);
        await _context.WorkingHours.AddRangeAsync(workingHours);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{NewBarberName} kadroya katıldı.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid barberId)
    {
        var barber = await _context.Barbers.FindAsync(barberId);
        if (barber != null) { barber.IsActive = !barber.IsActive; await _context.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteBarberAsync(Guid barberId)
    {
        // Berberi, ona bağlı çalışma saatlerini VE randevuları dahil ederek buluyoruz
        var barber = await _context.Barbers
            .Include(b => b.WorkingHours)
            .Include(b => b.Appointments) // Kritik nokta: Randevuları da dahil et
            .FirstOrDefaultAsync(b => b.Id == barberId);

        if (barber != null)
        {
            // 1. Önce berbere bağlı randevuları temizle (Hata veren kısım burasıydı)
            if (barber.Appointments.Any())
            {
                _context.Appointments.RemoveRange(barber.Appointments);
            }

            // 2. Çalışma saatlerini temizle
            if (barber.WorkingHours.Any())
            {
                _context.WorkingHours.RemoveRange(barber.WorkingHours);
            }

            // 3. Artık kimsesiz kalan berberi silebiliriz
            _context.Barbers.Remove(barber);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Berber, çalışma saatleri ve randevuları tamamen silindi.";
        }

        return RedirectToPage();
    }
}