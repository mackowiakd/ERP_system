using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetManager.Core.DBTables
{
    public enum SystemRole
    {
        Guest = 0,           // Nowo zarejestrowany, czeka na dołączenie do firmy lub jej utworzenie
        Employee = 1,        // Zwykły pracownik (widzi tylko swoje operacje / podstawowe widoki)
        Accountant = 2,      // Księgowy (dodaje faktury, generuje raporty, widzi finanse)
        CompanyAdmin = 3,    // Właściciel/Szef firmy (odpowiednik dawnego HouseholdAdmin - zarządza ludźmi)
        SystemAdmin = 4      // Globalny administrator (konsola SQL)
    }

    [Table("roles")]
    public class DBRole
    {
        // Używamy DatabaseGeneratedOption.None, bo chcemy, aby ID w bazie
        // dokładnie odpowiadało wartościom z Enuma (0, 1, 2...), a nie było autoincrement
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("role_id")]
        public SystemRole Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("role_name")]
        public required string Name { get; set; }
    }

    [Index(nameof(Email), IsUnique = true)]
    //[Table("employees")]
    public class DBEmployee
    {
        [Key]
        [Column("employee_id")]
        public int Id { get; set; }

        [Required]
        [Column("employee_email")]
        public required string Email { get; set; }

        [Required]
        [Column("employee_login")]
        public required string Login { get; set; }

        [Required]
        [Column("employee_password")]
        public required string Password { get; set; }

        [Column("employee_role")]
        public SystemRole Role { get; set; } = SystemRole.Guest;

        [Column("employee_company_id")]
        public int? CompanyId { get; set; }

        // Fix: Change navigation property type from DBEmployee? to the correct company entity type.
        // Assuming the company entity is named DBCorporation or similar. If not, please provide the correct type.
        // [ForeignKey(nameof(CompanyId))]
        // public DBCorporation? Company { get; set; }

        public ICollection<DBTransactionCategories> Categories { get; set; } = new List<DBTransactionCategories>();
        public ICollection<DBFinancialOperations> Transactions { get; set; } = new List<DBFinancialOperations>();
    }
}