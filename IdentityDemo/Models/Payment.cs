using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantsManagementApp.Models
{
    public class Payment : BaseEntity
    {
        [ForeignKey("Tenant")]
        public int TenantId { get; set; }

        [ForeignKey("House")]
        public int HouseId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [StringLength(255)]
        public string? TransactionReference { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigation Properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual House House { get; set; } = null!;
        public virtual ICollection<PaymentCharge> PaymentCharges { get; set; } = new List<PaymentCharge>();
    }
}
