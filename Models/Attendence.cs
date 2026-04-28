namespace Management.Models
{
    public class Attendance
    {
        public long Id { get; set; }
        public long EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime Date { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public string Status { get; set; } = "Present"; // Present, Absent, Late
    }
}
