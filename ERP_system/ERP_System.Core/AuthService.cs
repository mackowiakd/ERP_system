using ERP_System.Core.DBTables;

namespace ERP_System.Core;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly HashPassword _hasher = new();

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public bool ValidateUserByEmail(string email, string password)
    {
        var user = _context.Employees.FirstOrDefault(u => u.Email == email); // Poprawione na Employees
        if (user == null) return false;

        return _hasher.verifyPassword(user.Password, password);
    }

    public string GetWelcomeMessage(string username)
    {
        return $"Witaj w Mini-ERP, {username}! Twoje finanse są pod kontrolą.";
    }

    public DBEmployee? GetUserByUsername(string username)
    {
        return _context.Employees.FirstOrDefault(u => u.Login == username); // Poprawione
    }

    public DBEmployee? GetUserByEmail(string email)
    {
        return _context.Employees.FirstOrDefault(u => u.Email == email); // Poprawione
    }
}