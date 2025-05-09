using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Material_Control.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        public string? Role { get; set; }
        public string Name { get; set; }

        [NotMapped]
        public string? ErrorMessage { get; set; }
    }
}
