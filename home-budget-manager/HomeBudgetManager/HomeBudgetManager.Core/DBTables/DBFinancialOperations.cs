using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// TODO: finish all tables

namespace HomeBudgetManager.Core.DBTables
{
    public enum TransactionType
    {
        expense = 0,
        income = 1
    }

    [Table("FinancialOperations")]
    public class DBFinancialOperations
    {
        [Key]
        [Column("transaction_id")]
        public int Id { get; set; }

        [Column("category_id")]
        public required int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public DBTransactionCategories? Category { get; set; }

        [Column("company_id")]
        public required int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public DBEmployee? Company { get; set; }

        [Column("employee_id")]
        public required int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public DBEmployee? Employee { get; set; }

        [Required]
        [Column("transaction_value")]
        public decimal Value { get; set; }

        [Required]
        [Column("transaction_type")]
        public TransactionType TransactionType { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("transaction_title")]
        public required string Title { get; set; }

        [Column("transaction_description")]
        public string? Description { get; set; }

       

        [Required]
        [DataType(DataType.DateTime)]
        [Column("transaction_date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        [Column("transaction_is_repeatable")]
        public bool IsRepeatable { get; set; }

        public virtual DBRecurringOperations? RecurringOperation { get; set; }

        // --- NOWOŚĆ: Relacja do faktury ---
        [Column("invoice_id")]
        public int? InvoiceId { get; set; } // Nullable, bo nie każdy wydatek to faktura (np. kawa z kasy)

        [ForeignKey(nameof(InvoiceId))]
        public DBInvoice? Invoice { get; set; }
        // ----------------------------------

    }
}