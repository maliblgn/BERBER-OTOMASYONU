namespace SoftetroBarber.Models;

public class Customer
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public bool IsBlacklisted { get; set; }

    // Navigation Properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
