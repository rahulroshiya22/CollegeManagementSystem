using CMS.AcademicService.Data;
using CMS.AcademicService.DTOs;
using CMS.AcademicService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.AcademicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly AcademicDbContext _context;

        public MessageController(AcademicDbContext context)
        {
            _context = context;
        }

        [HttpGet("inbox/{userId}/{role}")]
        public async Task<IActionResult> GetInbox(int userId, string role)
        {
            var messages = await _context.Messages
                .Where(m => m.ReceiverId == userId && m.ReceiverRole == role)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("sent/{userId}/{role}")]
        public async Task<IActionResult> GetSent(int userId, string role)
        {
            var messages = await _context.Messages
                .Where(m => m.SenderId == userId && m.SenderRole == role)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto, [FromQuery] int senderId, [FromQuery] string senderRole)
        {
            var message = new Message
            {
                SenderId = senderId,
                SenderRole = senderRole,
                ReceiverId = dto.ReceiverId,
                ReceiverRole = dto.ReceiverRole,
                Subject = dto.Subject,
                Content = dto.Content,
                ParentMessageId = dto.ParentMessageId,
                AttachmentUrl = dto.AttachmentUrl,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = message.MessageId }, message);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();
            return Ok(message);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("unread-count/{userId}/{role}")]
        public async Task<IActionResult> GetUnreadCount(int userId, string role)
        {
            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && m.ReceiverRole == role && !m.IsRead);

            return Ok(new { unreadCount = count });
        }
    }
}
