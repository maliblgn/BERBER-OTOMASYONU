using System;
using System.ComponentModel.DataAnnotations;

namespace SoftetroBarber.Models;

public class Expense : BaseEntity
{
    [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
    [StringLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tutar alanı zorunludur.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Tarih alanı zorunludur.")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Kategori alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Kategori en fazla 100 karakter olabilir.")]
    public string Category { get; set; } = string.Empty;
}
