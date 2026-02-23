using CMS.CourseService.DTOs;
using CMS.CourseService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.CourseService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _service;

        public CourseController(ICourseService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _service.GetAllCoursesAsync();
            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _service.GetCourseByIdAsync(id);
            if (course == null) return NotFound(new { message = $"Course with ID {id} not found" });
            return Ok(course);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var courses = await _service.GetCoursesByDepartmentAsync(departmentId);
            return Ok(courses);
        }

        [HttpGet("semester/{semester}")]
        public async Task<IActionResult> GetBySemester(int semester)
        {
            var courses = await _service.GetCoursesBySemesterAsync(semester);
            return Ok(courses);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
        {
            var course = await _service.CreateCourseAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = course.CourseId }, course);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseDto dto)
        {
            var course = await _service.UpdateCourseAsync(id, dto);
            if (course == null) return NotFound(new { message = $"Course with ID {id} not found" });
            return Ok(course);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteCourseAsync(id);
            if (!deleted) return NotFound(new { message = $"Course with ID {id} not found" });
            return NoContent();
        }
    }
}
