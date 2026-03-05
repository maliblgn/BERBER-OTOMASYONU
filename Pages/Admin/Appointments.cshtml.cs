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

    public AppointmentsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Appointment> Appointments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Appointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
            
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment != null)
        {
            appointment.Status = newStatus;
            await _context.SaveChangesAsync();
        }
        
        return RedirectToPage();
    }
}
