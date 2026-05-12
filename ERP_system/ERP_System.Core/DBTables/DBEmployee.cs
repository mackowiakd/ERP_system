using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_System.Core.DBTables
{
    public enum SystemRole
    {
        Guest = 0,           // new user
        Employee = 1,        // regular employee who cannot delete a company
        Accountant = 2,      // ?? not implemented??
        CompanyAdmin = 3,    // A person who created a company
        SystemAdmin = 4      // System admin with admin console permission
    }

    [Table("roles")]
    public class DBRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("role_id")]
        public SystemRole Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("role_name")]
        public required string Name { get; set; }
    }

    [Table("employees")]
    [Index(nameof(Email), IsUnique = true)]
    
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

        [ForeignKey(nameof(CompanyId))]
        public DBCompany? Company { get; set; }

        public ICollection<DBInvoiceCategories> Categories { get; set; } = new List<DBInvoiceCategories>();
        public ICollection<DBFinancialOperations> Transactions { get; set; } = new List<DBFinancialOperations>();
    }
}