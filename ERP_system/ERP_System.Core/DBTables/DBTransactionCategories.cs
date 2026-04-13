using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_System.Core.DBTables
{

    [Table("transaction_categories")]
    public class DBInvoiceCategories

    {
        [Key]
        [Column("category_id")]
        public int Id { get; set; }

        [Column("company_id")]
        public int? CompanyId { get; set; } // Nullable, bo systemowe kategorie nie mają usera

        [ForeignKey(nameof(CompanyId))]
        public DBCompany? Company { get; set; }

        [Required]
        [Column("category_name")]
        public required string Name { get; set; }

        [Column("category_description")]
        public string? Description { get; set; }
    }

}
