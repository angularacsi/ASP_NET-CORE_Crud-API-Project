using Crud.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Crud.Models
{
    public class Product: IAuditableEntity
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        [StringLength(500)]
        public string? Description { get; set; }
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }
        [Range(0, 100000)]
        public int StockQuantity { get; set; }
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get ; set ; }
        public DateTime CreatedDate { get ; set; }
        public int? UpdatedBy { get; set ; }
        public DateTime? UpdatedDate { get ; set ; }
    }
}
