using CMS.AuthService.Data;
using CMS.AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AuthDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FirstName,
                u.LastName,
                Role = u.Role.ToString(),
                u.IsActive,
                u.PhotoUrl,
                u.ProfilePictureUrl,
                Provider = u.AuthProvider.ToString(),
                u.StudentId,
                u.TeacherId,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            Role = user.Role.ToString(),
            user.IsActive,
            user.PhotoUrl,
            user.ProfilePictureUrl,
            Provider = user.AuthProvider.ToString(),
            user.StudentId,
            user.TeacherId,
            user.CreatedAt
        });
    }

    /// <summary>
    /// Update user role (Admin only)
    /// </summary>
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            return BadRequest(new { message = "Invalid role. Valid roles: Student, Teacher, Admin" });

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin changed role of user {UserId} to {Role}", id, role);

        return Ok(new { message = $"User role updated to {role}", userId = id, role = role.ToString() });
    }

    /// <summary>
    /// Activate/Deactivate user (Admin only)
    /// </summary>
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"User {(request.IsActive ? "activated" : "deactivated")}", userId = id });
    }

    /// <summary>
    /// Delete user (Admin only)
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin deleted user {UserId} ({Email})", id, user.Email);

        return Ok(new { message = "User deleted successfully" });
    }

    /// <summary>
    /// Get dashboard stats (Admin only)
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var students = await _context.Users.CountAsync(u => u.Role == UserRole.Student);
        var teachers = await _context.Users.CountAsync(u => u.Role == UserRole.Teacher);
        var admins = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);

        return Ok(new
        {
            totalUsers,
            activeUsers,
            inactiveUsers = totalUsers - activeUsers,
            students,
            teachers,
            admins
        });
    }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    public bool IsActive { get; set; }
}
