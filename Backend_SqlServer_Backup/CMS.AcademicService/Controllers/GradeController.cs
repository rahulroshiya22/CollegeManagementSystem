using CMS.AcademicService.DTOs;
using CMS.AcademicService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AcademicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GradeController : ControllerBase
    {
        private readonly IGradeService _service;
        public GradeController(IGradeService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var grades = await _service.GetAllAsync();
            return Ok(grades);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var grade = await _service.GetByIdAsync(id);
            if (grade == null) return NotFound(new { message = $"Grade with ID {id} not found" });
            return Ok(grade);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var grades = await _service.GetByStudentAsync(studentId);
            return Ok(grades);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var grades = await _service.GetByCourseAsync(courseId);
            return Ok(grades);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGradeDto dto)
        {
            var grade = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = grade.GradeId }, grade);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGradeDto dto)
        {
            var grade = await _service.UpdateAsync(id, dto);
            if (grade == null) return NotFound(new { message = $"Grade with ID {id} not found" });
            return Ok(grade);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = $"Grade with ID {id} not found" });
            return NoContent();
        }
    }
}
