namespace Management.Models
{
    public class Employee
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public decimal Salary { get; set; }

        public long DepartmentId { get; set; }
        public Department? Department { get; set; }

        public long UserId { get; set; }
        public User? User { get; set; }

        // Supervisor relationship
        public long? SupervisorId { get; set; }
        public Employee? Supervisor { get; set; }

        // Inverse navigation for subordinates
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
    }
}