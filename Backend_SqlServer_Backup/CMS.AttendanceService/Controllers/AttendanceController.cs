using CMS.AttendanceService.DTOs;
using CMS.AttendanceService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AttendanceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;

        public AttendanceController(IAttendanceService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var records = await _service.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var record = await _service.GetByIdAsync(id);
            if (record == null) return NotFound(new { message = $"Attendance record with ID {id} not found" });
            return Ok(record);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var records = await _service.GetByStudentAsync(studentId);
            return Ok(records);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var records = await _service.GetByCourseAsync(courseId);
            return Ok(records);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAttendanceDto dto)
        {
            var record = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = record.AttendanceId }, record);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceDto dto)
        {
            var record = await _service.UpdateAsync(id, dto);
            if (record == null) return NotFound(new { message = $"Attendance record with ID {id} not found" });
            return Ok(record);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = $"Attendance record with ID {id} not found" });
            return NoContent();
        }
    }
}
