using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ERP_System.Core.DBTables
{
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
        public DBEmployee ? CompanyAdmin { get; set; }

        [Required]
        [Column("company_name")]
        public required string Name { get; set; }

        [Column("company_description")]
        public string? Description { get; set; }  // opis (opcjonalny)

        [Required]
        [Column("company_join_code")]
        public required string JoinCode { get; set; }

        public ICollection<DBEmployee> Members { get; set; } = new List<DBEmployee>();

        [Required]
        [Column("company_nip")]
        [MaxLength(15)] // Zawsze warto dawać limity na ciągi znaków!
        public required string NIP { get; set; }
    }
}
