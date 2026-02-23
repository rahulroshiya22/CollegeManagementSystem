namespace CMS.FeeService.DTOs
{
    public class CreateFeeDto
    {
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
    }

    public class UpdateFeeDto
    {
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
    }

    public class PayFeeDto
    {
        public DateTime PaidDate { get; set; } = DateTime.UtcNow;
    }
}
