using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin.Expenses;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Expense NewExpense { get; set; } = new Expense { Date = DateTime.Today };

    public List<Expense> Expenses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Expenses = await _context.Expenses
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            Expenses = await _context.Expenses
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .ToListAsync();
            return Page();
        }

        NewExpense.Id = Guid.NewGuid();
        await _context.Expenses.AddAsync(NewExpense);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid expenseId)
    {
        var expense = await _context.Expenses.FindAsync(expenseId);
        if (expense != null)
        {
            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
