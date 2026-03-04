using CMS.AcademicService.DTOs;
using CMS.AcademicService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AcademicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NoticeController : ControllerBase
    {
        private readonly INoticeService _service;
        public NoticeController(INoticeService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var notices = await _service.GetAllAsync();
            return Ok(notices);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] string? role)
        {
            var notices = await _service.GetActiveAsync(role);
            return Ok(notices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var notice = await _service.GetByIdAsync(id);
            if (notice == null) return NotFound(new { message = $"Notice with ID {id} not found" });
            return Ok(notice);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNoticeDto dto)
        {
            var notice = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = notice.NoticeId }, notice);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNoticeDto dto)
        {
            var notice = await _service.UpdateAsync(id, dto);
            if (notice == null) return NotFound(new { message = $"Notice with ID {id} not found" });
            return Ok(notice);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = $"Notice with ID {id} not found" });
            return NoContent();
        }
    }
}
