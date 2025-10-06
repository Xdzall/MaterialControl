using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Material_Control.Models
{
    public class PendingApprovalParts
    {
        [Key]
        public string IdentificationNo { get; set; }
        public string ItemPart { get; set; }
        public string CodePart { get; set; }
        public int Quantity { get; set; }
        public string StorageLocation { get; set; }
        public string Purpose { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PIC { get; set; }
        public string Status { get; set; }
        public string RequestType { get; set; }
        public string ProjectName { get; set; }
    }
}
