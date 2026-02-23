using CMS.AcademicService.DTOs;
using CMS.AcademicService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AcademicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeSlotController : ControllerBase
    {
        private readonly ITimeSlotService _service;
        public TimeSlotController(ITimeSlotService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var slots = await _service.GetAllAsync();
            return Ok(slots);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var slot = await _service.GetByIdAsync(id);
            if (slot == null) return NotFound(new { message = $"TimeSlot with ID {id} not found" });
            return Ok(slot);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var slots = await _service.GetByCourseAsync(courseId);
            return Ok(slots);
        }

        [HttpGet("teacher/{teacherId}")]
        public async Task<IActionResult> GetByTeacher(int teacherId)
        {
            var slots = await _service.GetByTeacherAsync(teacherId);
            return Ok(slots);
        }

        [HttpGet("day/{dayOfWeek}")]
        public async Task<IActionResult> GetByDay(string dayOfWeek)
        {
            var slots = await _service.GetByDayAsync(dayOfWeek);
            return Ok(slots);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTimeSlotDto dto)
        {
            var slot = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = slot.TimeSlotId }, slot);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTimeSlotDto dto)
        {
            var slot = await _service.UpdateAsync(id, dto);
            if (slot == null) return NotFound(new { message = $"TimeSlot with ID {id} not found" });
            return Ok(slot);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = $"TimeSlot with ID {id} not found" });
            return NoContent();
        }
    }
}
