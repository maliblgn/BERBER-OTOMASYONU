using Microsoft.AspNetCore.Authorization; //
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages; //
using Microsoft.EntityFrameworkCore; //
using SoftetroBarber.Data; //
using SoftetroBarber.Enums;
using SoftetroBarber.Models; //

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

    [BindProperty(SupportsGet = true)]
    public string CurrentSortColumn { get; set; } = "StartTime"; // Varsayılan sıralama

    [BindProperty(SupportsGet = true)]
    public string CurrentSortOrder { get; set; } = "asc"; // asc veya desc

    public async Task OnGetAsync()
    {
        BarbersList = await _context.Barbers.Where(b => b.IsActive).ToListAsync();

        var today = DateTime.Today;

        // 1. DÜZELTME: Sadece bugünün ve geleceğin randevuları.
        // Geçmiş randevular (dün ve öncesi) History sayfasına düşer.
        var query = _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .Where(a => a.StartTime.Date >= today)
            .AsQueryable();

        // Filtreleme ve Arama
        if (SelectedBarberId.HasValue && SelectedBarberId != Guid.Empty)
            query = query.Where(a => a.BarberId == SelectedBarberId);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            query = query.Where(a => a.Customer.FullName.Contains(SearchTerm) || a.Customer.PhoneNumber.Contains(SearchTerm));

        // 2. SIRALAMA (SORTING) MANTIĞI
        bool isDesc = CurrentSortOrder?.ToLower() == "desc";

        query = CurrentSortColumn switch
        {
            "Customer.FullName" => isDesc ? query.OrderByDescending(a => a.Customer.FullName) : query.OrderBy(a => a.Customer.FullName),
            "Barber.Name" => isDesc ? query.OrderByDescending(a => a.Barber.Name) : query.OrderBy(a => a.Barber.Name),
            "TotalPrice" => isDesc ? query.OrderByDescending(a => a.TotalPrice) : query.OrderBy(a => a.TotalPrice),
            "Status" => isDesc ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            // Varsayılan: Tarih ve Saat
            _ => isDesc ? query.OrderByDescending(a => a.StartTime) : query.OrderBy(a => a.StartTime)
        };

        // Verileri Çek
        Appointments = await query.ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment != null)
        {
            appointment.Status = newStatus;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Durum güncellendi.";
        }
        return RedirectToPage(new { SelectedBarberId, SearchTerm });
    }
}