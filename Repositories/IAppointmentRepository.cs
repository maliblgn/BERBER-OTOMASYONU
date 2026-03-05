using SoftetroBarber.Models;

namespace SoftetroBarber.Repositories;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetBarberAppointmentsByDateAsync(Guid barberId, DateTime date);
}
