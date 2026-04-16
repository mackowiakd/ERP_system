using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_System.Core.DBTables
{
    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        Transfer = 2
    }

    public enum InvoiceType
    {
        Sales = 0,
        Cost = 1
    }

    public enum InvoiceStatus
    {
        Unpaid = 0,
        Paid = 1,
        PartiallyPaid = 2
    }

    [Table("invoices")]
    public class DBInvoice
    {
        [Key]
        [Column("invoice_id")]
        public int Id { get; set; }

        [Required]
        [Column("company_id")]
        public required int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public DBCompany? Company { get; set; }

        [Required]
        [Column("contractor_id")]
        public required int ContractorId { get; set; }

        [ForeignKey(nameof(ContractorId))]
        public DBContractor? Contractor { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("invoice_number")]
        public required string InvoiceNumber { get; set; }

        [Required]
        [Column("issue_date")]
        public DateTime IssueDate { get; set; }

        [Required]
        [Column("due_date")]
        public DateTime DueDate { get; set; }

        [Required]
        [Column("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        [Column("total_net")]
        public decimal TotalNet { get; set; }

        [Required]
        [Column("total_gross")]
        public decimal TotalGross { get; set; }

        [Required]
        [Column("invoice_type")]
        public InvoiceType Type { get; set; }

        [Column("Notes")]
        public string? Notes { get; set; }

        [Required]
        [Column("invoice_status")]
        public InvoiceStatus Status { get; set; }

        public virtual DBRecurringOperations? RecurringOperation { get; set; }

        public ICollection<DBContractor>? RelatedContractorInvoices { get; set; } = new List<DBContractor>();
    }
}