using CMS.CourseService.Data;
using CMS.CourseService.DTOs;
using CMS.CourseService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.CourseService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly CourseDbContext _context;

        public DepartmentController(CourseDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _context.Departments.Include(d => d.Courses).ToListAsync();
            return Ok(departments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dept = await _context.Departments.Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);
            if (dept == null) return NotFound(new { message = $"Department with ID {id} not found" });
            return Ok(dept);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
        {
            var dept = new Department
            {
                Name = dto.Name,
                Code = dto.Code
            };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = dept.DepartmentId }, dept);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto dto)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound(new { message = $"Department with ID {id} not found" });

            if (dto.Name != null) dept.Name = dto.Name;
            if (dto.Code != null) dept.Code = dto.Code;
            if (dto.IsActive.HasValue) dept.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return Ok(dept);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound(new { message = $"Department with ID {id} not found" });
            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
