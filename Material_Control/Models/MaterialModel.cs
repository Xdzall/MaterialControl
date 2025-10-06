using System.ComponentModel.DataAnnotations;

namespace Material_Control.Models
{
    public class MaterialModel
    {
        [Key]
        public string IdentificationNo { get; set; }

        [Required]
        public string ItemPart { get; set; }

        [Required]
        public string CodePart { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string StorageLocation { get; set; }

        [Required]
        public string Purpose { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public string PIC { get; set; }

        public string Status { get; set; }

        public string RequestType { get; set; }

        public string ProjectName { get; set; }
    }
}
