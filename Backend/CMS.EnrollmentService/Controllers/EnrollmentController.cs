using CMS.EnrollmentService.DTOs;
using CMS.EnrollmentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.EnrollmentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _service;

        public EnrollmentController(IEnrollmentService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var enrollments = await _service.GetAllAsync();
            return Ok(enrollments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var enrollment = await _service.GetByIdAsync(id);
            if (enrollment == null) return NotFound(new { message = $"Enrollment with ID {id} not found" });
            return Ok(enrollment);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var enrollments = await _service.GetByStudentAsync(studentId);
            return Ok(enrollments);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var enrollments = await _service.GetByCourseAsync(courseId);
            return Ok(enrollments);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEnrollmentDto dto)
        {
            var enrollment = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = enrollment.EnrollmentId }, enrollment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEnrollmentDto dto)
        {
            var enrollment = await _service.UpdateAsync(id, dto);
            if (enrollment == null) return NotFound(new { message = $"Enrollment with ID {id} not found" });
            return Ok(enrollment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = $"Enrollment with ID {id} not found" });
            return NoContent();
        }
    }
}
