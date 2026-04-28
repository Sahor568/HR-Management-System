using System.Linq;

namespace Management.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee"; // Admin, HR, Employee

        public static readonly string[] ValidRoles = { "Admin", "HR", "Employee" };

        public bool IsValidRole()
        {
            return ValidRoles.Contains(Role);
        }
    }
}


