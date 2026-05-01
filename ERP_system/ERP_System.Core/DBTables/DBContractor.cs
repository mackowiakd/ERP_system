using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_System.Core.DBTables
{
    [Table("contractors")]
    public class DBContractor
    {
        [Column("contractor_id")]
        public int Id { get; set; }
        
        [Required]
        [Column("contractor_name")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Column("contractor_tax_id")]
        public string TaxId { get; set; } = string.Empty; // NIP

        [Column("company_id")]
        public int CompanyId { get; set; }

        // --- NASZE NOWE POLA (EF dodał je z domyślnymi nazwami, więc nie potrzebują [Column]) ---
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        public virtual DBCompany? Company { get; set; }
    }
}