using CMS.AcademicService.Data;
using CMS.AcademicService.DTOs;
using CMS.AcademicService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.AcademicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementController : ControllerBase
    {
        private readonly AcademicDbContext _context;

        public AnnouncementController(AcademicDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var announcements = await _context.GroupAnnouncements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(announcements);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var announcement = await _context.GroupAnnouncements.FindAsync(id);
            if (announcement == null) return NotFound();
            return Ok(announcement);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto, [FromQuery] int creatorId, [FromQuery] string creatorRole)
        {
            var announcement = new GroupAnnouncement
            {
                CreatorId = creatorId,
                CreatorRole = creatorRole,
                Title = dto.Title,
                Content = dto.Content,
                TargetAudience = dto.TargetAudience,
                TargetFilter = dto.TargetFilter,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.GroupAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = announcement.AnnouncementId }, announcement);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateAnnouncementDto dto)
        {
            var announcement = await _context.GroupAnnouncements.FindAsync(id);
            if (announcement == null) return NotFound();

            announcement.Title = dto.Title;
            announcement.Content = dto.Content;
            announcement.TargetAudience = dto.TargetAudience;
            announcement.TargetFilter = dto.TargetFilter;

            await _context.SaveChangesAsync();
            return Ok(announcement);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var announcement = await _context.GroupAnnouncements.FindAsync(id);
            if (announcement == null) return NotFound();

            announcement.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
