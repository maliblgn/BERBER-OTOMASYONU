using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;

namespace SoftetroBarber.Repositories;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Appointment>> GetBarberAppointmentsByDateAsync(Guid barberId, DateTime date)
    {
        return await _dbSet
            .Include(a => a.AppointmentServices)
            .Where(a => a.BarberId == barberId 
                        && a.StartTime.Date == date.Date 
                        && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();
    }
}
