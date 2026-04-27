using System.ComponentModel.DataAnnotations;

namespace Management.Models
{
    public class HR
    {
        public long Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public int Age { get; set; }

        public string Location { get; set; } = string.Empty;
    }
}