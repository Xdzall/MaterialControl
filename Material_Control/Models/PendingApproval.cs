using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Material_Control.Models
{
    public class PendingApproval
    {
        [Key]
        public string IdentificationNo { get; set; }

        [NotMapped]
        public string ItemPart { get; set; }

        public string ModelName { get; set; }

        [NotMapped]
        public string CodePart { get; set; }

        public int Quantity { get; set; } // Kembalikan ke int

        public string StorageLocation { get; set; }

        public string Purpose { get; set; }

        public DateTime CreatedAt { get; set; }

        public string PIC { get; set; }

        public string Status { get; set; }

        public string RequestType { get; set; }

        public string SP_Number { get; set; }

        public string ProjectName { get; set; }
    }
}