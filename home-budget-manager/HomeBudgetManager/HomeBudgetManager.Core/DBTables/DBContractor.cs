using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetManager.Core.DBTables
{
    [Table("contractors")]
    public class DBContractor
    {
        [Key]
        [Column("contractor_id")]
        public int Id { get; set; }

        [Required]
        [Column("company_id")]
        public required int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public DBCompany? Company { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("contractor_name")]
        public required string Name { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("contractor_tax_id")]
        public required string TaxId { get; set; }

        public ICollection<DBInvoice> Invoices { get; set; } = new List<DBInvoice>();
    }
}