using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ERP_System.Core.DBTables
{
    /// <summary>
    /// Represents a company entity in the system.
    /// </summary>
    [Table("company")]
    public class DBCompany
    {
        [Key]
        [Column("company_id")]
        public int Id { get; set; }

        [Required]
        [Column("company_admin_id")]
        public int CompanyAdminId { get; set; }

        [ForeignKey(nameof(CompanyAdminId))]
        public DBEmployee? CompanyAdmin { get; set; }

        [Required]
        [Column("company_short_name")]
        [MaxLength(100)]
        public required string ShortName { get; set; }

        [Required]
        [Column("company_full_name")]
        [MaxLength(500)]
        public required string FullName { get; set; }

        [Required]
        [Column("company_address")]
        [MaxLength(500)]
        public required string Address { get; set; }

        [Column("company_description")]
        public string? Description { get; set; }

        [Required]
        [Column("company_join_code")]
        public required string JoinCode { get; set; }

        public ICollection<DBEmployee> Members { get; set; } = new List<DBEmployee>();

        [Required]
        [Column("company_nip")]
        [MaxLength(15)]
        public required string NIP { get; set; }
    }
}
