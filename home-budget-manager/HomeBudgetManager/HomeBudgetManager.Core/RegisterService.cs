using HomeBudgetManager.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace HomeBudgetManager.Core;

public class RegisterService
{
    private readonly AppDbContext _context;
    private readonly HashPassword _hasher = new();

    public RegisterService(AppDbContext context)
    {
        _context = context;
    }

    public bool IsEmailTaken(string email)
    {
        return _context.Employees.Any(u => u.Email == email); // Poprawione na Employees
    }

    public void RegisterUser(string email, string username, string password)
    {
        var user = new DBEmployee
        {
            Email = email,
            Login = username,
            Password = _hasher.hash(password),
            Role = SystemRole.Guest
        };

        _context.Employees.Add(user); // Poprawione na Employees
        _context.SaveChanges();
    }
}