namespace Management.Models
{
    public class Payroll
    {
        public long Id { get; set; }

        public long EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public decimal BasicSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deductions { get; set; }

        public decimal NetSalary { get; set; }
    }
}
