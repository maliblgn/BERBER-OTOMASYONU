namespace SoftetroBarber.Models;

public class Service
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int DurationInMinutes { get; set; }
    public decimal Price { get; set; }

    // Navigation Properties
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}
