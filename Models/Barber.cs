namespace SoftetroBarber.Models;

public class Barber
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Bio { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<TimeOffs> TimeOffs { get; set; } = new List<TimeOffs>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
