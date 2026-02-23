using CMS.AttendanceService.Models;
using CMS.AttendanceService.Repositories;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CMS.AttendanceService.Controllers
{
    /// <summary>
    /// Attendance Controller using DAPPER for database operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceDapperController : ControllerBase
    {
        private readonly IAttendanceDapperRepository _repository;

        public AttendanceDapperController(IAttendanceDapperRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Get all attendance records (Dapper)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAll()
        {
            var attendances = await _repository.GetAllAsync();
            return Ok(attendances);
        }

        /// <summary>
        /// Get attendance by ID (Dapper)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Attendance>> GetById(int id)
        {
            var attendance = await _repository.GetByIdAsync(id);
            if (attendance == null)
                return NotFound(new { message = $"Attendance record with ID {id} not found" });
            return Ok(attendance);
        }

        /// <summary>
        /// Get attendance by Student ID (Dapper)
        /// </summary>
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetByStudent(int studentId)
        {
            var attendances = await _repository.GetByStudentIdAsync(studentId);
            return Ok(attendances);
        }

        /// <summary>
        /// Get attendance by Course ID (Dapper)
        /// </summary>
        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetByCourse(int courseId)
        {
            var attendances = await _repository.GetByCourseIdAsync(courseId);
            return Ok(attendances);
        }

        /// <summary>
        /// Create new attendance record (Dapper)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Attendance>> Create([FromBody] Attendance attendance)
        {
            var id = await _repository.CreateAsync(attendance);
            attendance.AttendanceId = id;
            return CreatedAtAction(nameof(GetById), new { id }, attendance);
        }

        /// <summary>
        /// Update attendance record (Dapper)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Attendance attendance)
        {
            attendance.AttendanceId = id;
            var updated = await _repository.UpdateAsync(attendance);
            if (!updated)
                return NotFound(new { message = $"Attendance record with ID {id} not found" });
            return NoContent();
        }

        /// <summary>
        /// Delete attendance record (Dapper)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Attendance record with ID {id} not found" });
            return NoContent();
        }

        /// <summary>
        /// Bulk insert attendance records using JSON (SqlBulkCopy)
        /// </summary>
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> BulkInsert([FromBody] List<Attendance> attendances)
        {
            if (attendances == null || attendances.Count == 0)
                return BadRequest(new { message = "No attendance records provided" });

            var count = await _repository.BulkInsertAsync(attendances);
            return Ok(new { message = $"Successfully bulk inserted {count} attendance records", count });
        }

        /// <summary>
        /// Bulk insert from CSV file. Columns: StudentId, CourseId, Date, IsPresent, Remarks
        /// </summary>
        [HttpPost("bulk/csv")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> BulkInsertFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only CSV files are allowed" });

            try
            {
                var attendances = new List<Attendance>();
                
                using var reader = new StreamReader(file.OpenReadStream());
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                };
                using var csv = new CsvReader(reader, config);

                attendances = csv.GetRecords<AttendanceCsvDto>()
                    .Select(dto => new Attendance
                    {
                        StudentId = dto.StudentId,
                        CourseId = dto.CourseId,
                        Date = dto.Date,
                        IsPresent = dto.IsPresent,
                        Remarks = dto.Remarks
                    })
                    .ToList();

                if (attendances.Count == 0)
                    return BadRequest(new { message = "No valid records found in CSV" });

                var count = await _repository.BulkInsertAsync(attendances);
                return Ok(new { message = $"Successfully imported {count} records from CSV", count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error parsing CSV: {ex.Message}" });
            }
        }

        /// <summary>
        /// Bulk insert from Excel file (.xlsx). Columns: StudentId, CourseId, Date, IsPresent, Remarks
        /// </summary>
        [HttpPost("bulk/excel")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> BulkInsertFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only Excel (.xlsx) files are allowed" });

            try
            {
                var attendances = new List<Attendance>();

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header row

                foreach (var row in rows)
                {
                    var attendance = new Attendance
                    {
                        StudentId = row.Cell(1).GetValue<int>(),
                        CourseId = row.Cell(2).GetValue<int>(),
                        Date = row.Cell(3).GetValue<DateTime>(),
                        IsPresent = row.Cell(4).GetValue<bool>(),
                        Remarks = row.Cell(5).GetString()
                    };
                    attendances.Add(attendance);
                }

                if (attendances.Count == 0)
                    return BadRequest(new { message = "No valid records found in Excel" });

                var count = await _repository.BulkInsertAsync(attendances);
                return Ok(new { message = $"Successfully imported {count} records from Excel", count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error parsing Excel: {ex.Message}" });
            }
        }

        /// <summary>
        /// Download sample CSV template
        /// </summary>
        [HttpGet("template/csv")]
        public IActionResult DownloadCsvTemplate()
        {
            var csv = "StudentId,CourseId,Date,IsPresent,Remarks\n1,1,2025-01-01,true,Present\n2,1,2025-01-01,false,Absent";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "attendance_template.csv");
        }

        /// <summary>
        /// Download sample Excel template
        /// </summary>
        [HttpGet("template/excel")]
        public IActionResult DownloadExcelTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            // Headers
            worksheet.Cell(1, 1).Value = "StudentId";
            worksheet.Cell(1, 2).Value = "CourseId";
            worksheet.Cell(1, 3).Value = "Date";
            worksheet.Cell(1, 4).Value = "IsPresent";
            worksheet.Cell(1, 5).Value = "Remarks";

            // Sample data
            worksheet.Cell(2, 1).Value = 1;
            worksheet.Cell(2, 2).Value = 1;
            worksheet.Cell(2, 3).Value = DateTime.Now;
            worksheet.Cell(2, 4).Value = true;
            worksheet.Cell(2, 5).Value = "Present";

            worksheet.Cell(3, 1).Value = 2;
            worksheet.Cell(3, 2).Value = 1;
            worksheet.Cell(3, 3).Value = DateTime.Now;
            worksheet.Cell(3, 4).Value = false;
            worksheet.Cell(3, 5).Value = "Absent";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attendance_template.xlsx");
        }
    }

    // DTO for CSV mapping with CsvHelper Name attributes
    public class AttendanceCsvDto
    {
        [CsvHelper.Configuration.Attributes.Name("StudentId", "studentId", "student_id")]
        public int StudentId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("CourseId", "courseId", "course_id")]
        public int CourseId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Date", "date")]
        public DateTime Date { get; set; }

        [CsvHelper.Configuration.Attributes.Name("IsPresent", "isPresent", "is_present")]
        public bool IsPresent { get; set; }

        [CsvHelper.Configuration.Attributes.Name("Remarks", "remarks")]
        public string? Remarks { get; set; }
    }
}
