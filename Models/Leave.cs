namespace Management.Models
{
    public class Leave
    {
        public long Id { get; set; }

        public long EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        // Approval tracking
        public long? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalRemarks { get; set; }
    }
}
