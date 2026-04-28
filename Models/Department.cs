namespace Management.Models
{
    public class Department
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
