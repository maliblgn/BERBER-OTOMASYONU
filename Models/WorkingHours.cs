using System;

namespace SoftetroBarber.Models;

public class WorkingHours
{
    public Guid Id { get; set; }

    // Nullable yaptık: Eğer null ise "Dükkan Genel" saatidir.
    public Guid? BarberId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }

    // Yeni eklediğimiz alan: Tatil günlerini belirlemek için.
    public bool IsClosed { get; set; }

    // Navigation Properties
    // Barber? yaparak ilişkinin opsiyonel olduğunu belirttik.
    public Barber? Barber { get; set; }
}