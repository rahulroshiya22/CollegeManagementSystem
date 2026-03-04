using CMS.Common.Messaging;
using CMS.FeeService.Data;
using CMS.FeeService.Models;
using System.Text.Json;

namespace CMS.FeeService.Messaging
{
    public class StudentEnrolledSubscriber : MessageSubscriber
    {
        private readonly IServiceProvider _serviceProvider;

        public StudentEnrolledSubscriber(IConfiguration config, IServiceProvider serviceProvider)
            : base(config["CloudAMQP:ConnectionString"]!, "student-enrolled")
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ProcessMessageAsync(string message)
        {
            try
            {
                var data = JsonSerializer.Deserialize<StudentEnrolledEvent>(message);
                if (data == null) return;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FeeDbContext>();
                    
                    // Auto-generate fee for enrolled student
                    var fee = new Fee
                    {
                        StudentId = data.StudentId,
                        Amount = 5000, // Default semester fee
                        Description = $"Semester {data.Semester} Fee - Year {data.Year}",
                        Status = "Pending",
                        DueDate = DateTime.UtcNow.AddMonths(1)
                    };

                    context.Fees.Add(fee);
                    await context.SaveChangesAsync();
                    
                    Console.WriteLine($"---> Fee auto-generated for Student {data.StudentId}: ${fee.Amount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing enrollment event: {ex.Message}");
            }
        }
    }

    public class StudentEnrolledEvent
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int Semester { get; set; }
        public int Year { get; set; }
    }
}
