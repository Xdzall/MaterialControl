using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Material_Control.Models
{
    public class InventoryItemModel
    {
        [Key]
        public string IdentificationNo { get; set; }

        [NotMapped]
        public string ItemPart { get; set; }

        public string ModelName { get; set; }

        [NotMapped] // Properti ini tidak akan dipetakan ke database
        public string CodePart { get; set; } // Untuk Parts & Materials

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string StorageLocation { get; set; }

        [Required]
        public string Purpose { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public string PIC { get; set; }

        public string RequestType { get; set; }

        public string Status { get; set; }

        public string SP_Number { get; set; }

        public string ProjectName { get; set; }

        public string? Borrower { get; set; }

    }
}