using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AppointmentsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public AppointmentsModel(ApplicationDbContext context) { _context = context; }

    public List<Appointment> Appointments { get; set; } = new();
    public List<Barber> BarbersList { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedBarberId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        // Aktif personelleri çekiyoruz
        BarbersList = await _context.Barbers.Where(b => b.IsActive).ToListAsync();

        var now = DateTime.Now; // Şu anki zamanı alıyoruz

        var query = _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .AsQueryable();

        // KRİTİK FİLTRE: Sadece bitiş saati şu andan sonra olanları getir
        // Bu satır sayesinde 7 Mart veya saati geçmiş bugünün randevuları listeden kalkar.
        query = query.Where(a => a.EndTime >= now);

        // İsim veya Telefon ile Arama
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(a => a.Customer.FullName.Contains(SearchTerm) ||
                                     a.Customer.PhoneNumber.Contains(SearchTerm));
        }

        // Berber Filtrelemesi
        if (SelectedBarberId.HasValue && SelectedBarberId != Guid.Empty)
        {
            query = query.Where(a => a.BarberId == SelectedBarberId);
        }

        // Kronolojik olarak sırala
        Appointments = await query
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment != null)
        {
            appointment.Status = newStatus;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Randevu başarıyla güncellendi.";
        }
        return RedirectToPage(new { SelectedBarberId, SearchTerm });
    }
}