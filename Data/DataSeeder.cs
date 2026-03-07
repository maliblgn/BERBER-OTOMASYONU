using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Models;

namespace SoftetroBarber.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        // 1. Hizmetleri Ekle (Sadece olmayanları)
        var newServices = new List<Service>
        {
            new Service { Id = Guid.NewGuid(), Name = "Saç Kesimi", DurationInMinutes = 30, Price = 200 },
            new Service { Id = Guid.NewGuid(), Name = "Sakal Tıraşı", DurationInMinutes = 15, Price = 100 },
            new Service { Id = Guid.NewGuid(), Name = "Saç & Sakal Yıkama", DurationInMinutes = 45, Price = 280 },
            new Service { Id = Guid.NewGuid(), Name = "Cilt Bakımı", DurationInMinutes = 20, Price = 150 },
            new Service { Id = Guid.NewGuid(), Name = "Keratin Yüklemesi", DurationInMinutes = 45, Price = 400 },
            new Service { Id = Guid.NewGuid(), Name = "Saç Boyama", DurationInMinutes = 60, Price = 600 },
            new Service { Id = Guid.NewGuid(), Name = "Damat Tıraşı", DurationInMinutes = 90, Price = 1500 }
        };

        foreach (var svc in newServices)
        {
            if (!await context.Services.AnyAsync(s => s.Name == svc.Name))
            {
                await context.Services.AddAsync(svc);
            }
        }
        await context.SaveChangesAsync();

        // 2. Berberleri Ekle (Yoksa)
        if (!await context.Barbers.AnyAsync())
        {
            var aliId = Guid.NewGuid();
            var yasincanId = Guid.NewGuid();

            var barbers = new List<Barber>
            {
                new Barber { Id = aliId, Name = "Ali", Bio = "Kurucu/Usta Makas", IsActive = true },
                new Barber { Id = yasincanId, Name = "Yasincan", Bio = "Kurucu/Modern Kesimler", IsActive = true }
            };

            await context.Barbers.AddRangeAsync(barbers);

            // Çalışma Saatlerini Ekle (Pazartesi - Cumartesi : 1 - 6)
            var workingHours = new List<WorkingHours>();
            var openTime = new TimeSpan(9, 0, 0);   // 09:00
            var closeTime = new TimeSpan(19, 0, 0); // 19:00

            foreach (var barber in barbers)
            {
                for (int day = 1; day <= 6; day++)
                {
                    workingHours.Add(new WorkingHours
                    {
                        Id = Guid.NewGuid(),
                        BarberId = barber.Id,
                        DayOfWeek = (DayOfWeek)day,
                        OpenTime = openTime,
                        CloseTime = closeTime
                    });
                }
            }

            await context.WorkingHours.AddRangeAsync(workingHours);
            await context.SaveChangesAsync();
        }
        // 3. Mevcut berberlerden Çalışma Saati (WorkingHours) olmayanlara geriye dönük ekleme yap
        var barbersWithoutHours = await context.Barbers
            .Include(b => b.WorkingHours)
            .Where(b => !b.WorkingHours.Any())
            .ToListAsync();

        if (barbersWithoutHours.Any())
        {
            var openTime = new TimeSpan(9, 0, 0);
            var closeTime = new TimeSpan(19, 0, 0);
            var missingWorkingHours = new List<WorkingHours>();

            foreach (var barber in barbersWithoutHours)
            {
                for (int day = 1; day <= 6; day++) // Pazartesi(1) - Cumartesi(6)
                {
                    missingWorkingHours.Add(new WorkingHours
                    {
                        Id = Guid.NewGuid(),
                        BarberId = barber.Id,
                        DayOfWeek = (DayOfWeek)day,
                        OpenTime = openTime,
                        CloseTime = closeTime
                    });
                }
            }
            await context.WorkingHours.AddRangeAsync(missingWorkingHours);
            await context.SaveChangesAsync();
        }
    }
}
