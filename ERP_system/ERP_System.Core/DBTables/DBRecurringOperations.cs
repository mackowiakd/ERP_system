using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_System.Core.DBTables
{
    [Table("repetable_transactions")]
    public class DBRecurringOperations
    {
        [Key]
        [Column("pattern_id")]
        public int TransactionPatternId { get; set; }

        [Required]
        [Column("interval_value")]
        public int IntervalValue { get; set; }

       

        public bool IsActive { get; set; } = true;

        [Column("next_run_date")]
        public DateTime NextRunDate { get; set; }

       

        [Column("IntervalType")] 
        public int IntervalType { get; set; } // np. 0=Dni, 1=Tygodnie, 2=Miesiące, 3=Lata

        [ForeignKey(nameof(TransactionPatternId))]
        public virtual DBFinancialOperations? Invoice { get; set; }
    }
}