using System.Linq;
using System.Text.Json.Serialization;

namespace Management.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee"; // Admin, HR, Employee
        public bool IsMainAdmin { get; set; } = false; // Only one user should have this true

        public static readonly string[] ValidRoles = { "Admin", "HR", "Employee" };

        public bool IsValidRole()
        {
            return ValidRoles.Contains(Role);
        }
    }
}


