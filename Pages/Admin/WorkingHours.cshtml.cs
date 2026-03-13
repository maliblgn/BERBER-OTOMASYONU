using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class WorkingHoursModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public WorkingHoursModel(ApplicationDbContext context) => _context = context;

    public List<WorkingHours> ShopHours { get; set; } = new();
    public List<Barber> Barbers { get; set; } = new();

    public async Task OnGetAsync()
    {
        Barbers = await _context.Barbers.ToListAsync();
        ShopHours = await _context.WorkingHours
            .Where(wh => wh.BarberId == null)
            .OrderBy(wh => wh.DayOfWeek).ToListAsync();
    }

    // AJAX ile çağrılacak metod
    public async Task<JsonResult> OnPostUpdateRowAsync([FromBody] WorkingHoursUpdateDto data)
    {
        var record = await _context.WorkingHours.FindAsync(data.Id);
        if (record == null) return new JsonResult(new { success = false });

        record.OpenTime = data.OpenTime;
        record.CloseTime = data.CloseTime;
        record.IsClosed = data.IsClosed;

        await _context.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }

    // Berber saatlerini getiren metod (Seçime göre yükleme için)
    public async Task<JsonResult> OnGetBarberHoursAsync(Guid barberId)
    {
        // Dükkanın kapalı olduğu günleri bul (Index listesi: 0, 1, 5 vb.)
        var closedShopDays = await _context.WorkingHours
            .Where(wh => wh.BarberId == null && wh.IsClosed)
            .Select(wh => (int)wh.DayOfWeek)
            .ToListAsync();

        // Berber saatlerini çek ama dükkanın kapalı olduğu günleri LİSTEDEN ÇIKAR
        var hours = await _context.WorkingHours
            .Where(wh => wh.BarberId == barberId && !closedShopDays.Contains((int)wh.DayOfWeek))
            .OrderBy(wh => wh.DayOfWeek)
            .Select(h => new { h.Id, h.DayOfWeek, h.OpenTime, h.CloseTime, h.IsClosed })
            .ToListAsync();

        return new JsonResult(hours);
    }
}

public class WorkingHoursUpdateDto
{
    public Guid Id { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}