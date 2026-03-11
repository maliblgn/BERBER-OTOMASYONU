using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftetroBarber.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ServicesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ServicesModel(ApplicationDbContext context) => _context = context;

        public List<SoftetroBarber.Models.Service> ServicesList { get; set; } = new();

        public async Task OnGetAsync()
        {
            ServicesList = await _context.Services.OrderBy(s => s.Name).ToListAsync();
        }

        // EKLEME METODU - Parametreleri doğrudan alarak hata riskini sıfırladık
        public async Task<IActionResult> OnPostAddServiceAsync(string NewName, decimal NewPrice, int NewDuration)
        {
            if (string.IsNullOrWhiteSpace(NewName)) return RedirectToPage();

            var service = new SoftetroBarber.Models.Service
            {
                Id = Guid.NewGuid(),
                Name = NewName.ToUpper(),
                Price = NewPrice,
                DurationInMinutes = NewDuration
            };

            await _context.Services.AddAsync(service);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yeni hizmet başarıyla portföye eklendi.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteServiceAsync(Guid serviceId)
        {
            var service = await _context.Services
                .Include(s => s.AppointmentServices)
                .FirstOrDefaultAsync(s => s.Id == serviceId);

            if (service != null)
            {
                if (service.AppointmentServices != null && service.AppointmentServices.Any())
                    _context.AppointmentServices.RemoveRange(service.AppointmentServices);

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<JsonResult> OnPostAutoUpdateAsync(Guid serviceId, decimal? price, int? duration)
        {
            try
            {
                var service = await _context.Services.FindAsync(serviceId);
                if (service == null) return new JsonResult(new { success = false });
                if (price.HasValue) service.Price = price.Value;
                if (duration.HasValue) service.DurationInMinutes = duration.Value;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch { return new JsonResult(new { success = false }); }
        }
    }
}