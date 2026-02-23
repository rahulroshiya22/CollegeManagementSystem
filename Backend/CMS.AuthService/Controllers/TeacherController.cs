using CMS.AuthService.DTOs;
using CMS.AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacherService;
    private readonly ILogger<TeacherController> _logger;

    public TeacherController(ITeacherService teacherService, ILogger<TeacherController> logger)
    {
        _teacherService = teacherService;
        _logger = logger;
    }

    // GET: api/Teacher
    [HttpGet]
    public async Task<ActionResult<object>> GetAll([FromQuery] TeacherQueryDto query)
    {
        var (teachers, totalCount) = await _teacherService.GetAllTeachersAsync(query);
        return Ok(new
        {
            data = teachers,
            totalCount,
            page = query.Page,
            pageSize = query.PageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        });
    }

    // GET: api/Teacher/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TeacherResponseDto>> GetById(int id)
    {
        var teacher = await _teacherService.GetTeacherByIdAsync(id);
        if (teacher == null)
            return NotFound(new { message = $"Teacher with ID {id} not found" });

        return Ok(teacher);
    }

    // GET: api/Teacher/user/5
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<TeacherResponseDto>> GetByUserId(int userId)
    {
        var teacher = await _teacherService.GetTeacherByUserIdAsync(userId);
        if (teacher == null)
            return NotFound(new { message = $"Teacher with UserID {userId} not found" });

        return Ok(teacher);
    }

    // POST: api/Teacher
    [HttpPost]
    public async Task<ActionResult<TeacherResponseDto>> Create([FromBody] CreateTeacherDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var teacher = await _teacherService.CreateTeacherAsync(dto);
            if (teacher == null)
                return BadRequest(new { message = "Email already exists" });

            return CreatedAtAction(nameof(GetById), new { id = teacher.TeacherId }, teacher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating teacher");
            return StatusCode(500, new { message = "An error occurred while creating the teacher" });
        }
    }

    // PUT: api/Teacher/5
    [HttpPut("{id}")]
    public async Task<ActionResult<TeacherResponseDto>> Update(int id, [FromBody] UpdateTeacherDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var teacher = await _teacherService.UpdateTeacherAsync(id, dto);
        if (teacher == null)
            return NotFound(new { message = $"Teacher with ID {id} not found" });

        return Ok(teacher);
    }

    // DELETE: api/Teacher/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _teacherService.DeleteTeacherAsync(id);
        if (!result)
            return NotFound(new { message = $"Teacher with ID {id} not found" });

        return Ok(new { message = "Teacher deactivated successfully" });
    }

    // GET: api/Teacher/department/Computer Science
    [HttpGet("department/{department}")]
    public async Task<ActionResult<IEnumerable<TeacherResponseDto>>> GetByDepartment(string department)
    {
        var teachers = await _teacherService.GetTeachersByDepartmentAsync(department);
        return Ok(teachers);
    }
}
