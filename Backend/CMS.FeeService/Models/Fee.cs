namespace CMS.FeeService.Models
{
    public class Fee
    {
        public int FeeId { get; set; }
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
