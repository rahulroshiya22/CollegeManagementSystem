using CMS.AuthService.DTOs;
using CMS.AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IGoogleAuthService googleAuthService,
        IFileUploadService fileUploadService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.LoginAsync(request);
        
        if (response == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RegisterAsync(request);
        
        if (response == null)
            return BadRequest(new { message = "Email already exists" });

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken);
        
        if (response == null)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RevokeTokenAsync(request.RefreshToken);
        
        if (!result)
            return BadRequest(new { message = "Invalid refresh token" });

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var response = await _googleAuthService.GoogleLoginAsync(request.Code);
        
        if (response == null)
            return Unauthorized(new { message = "Invalid Google token" });

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId);
        
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            Role = user.Role.ToString(),
            PhotoUrl = user.PhotoUrl ?? user.ProfilePictureUrl,
            user.CreatedAt
        });
    }

    [HttpPost("upload-photo")]
    [Authorize]
    public async Task<IActionResult> UploadPhoto(IFormFile photo)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId) || roleClaim == null)
            return Unauthorized();

        var photoUrl = await _fileUploadService.UploadPhotoAsync(photo, userId, roleClaim.Value);
        
        if (photoUrl == null)
            return BadRequest(new { message = "Failed to upload photo. Ensure file is JPG/PNG and under 5MB." });

        // Update user's photo URL
        var user = await _authService.GetUserByIdAsync(userId);
        if (user != null)
        {
            user.PhotoUrl = photoUrl;
            user.UpdatedAt = DateTime.UtcNow;
            // Save changes would happen in a proper update method
        }

        return Ok(new { photoUrl });
    }

    [HttpDelete("delete-photo")]
    [Authorize]
    public async Task<IActionResult> DeletePhoto()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PhotoUrl))
            return NotFound(new { message = "No photo to delete" });

        var deleted = _fileUploadService.DeletePhoto(user.PhotoUrl);
        
        if (deleted)
        {
            user.PhotoUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            // Save changes would happen in a proper update method
        }

        return Ok(new { message = "Photo deleted successfully" });
    }

    // ─── Admin User Management ───

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request?.Email) || string.IsNullOrEmpty(request?.NewPassword))
            return BadRequest(new { message = "Email and new password are required" });

        var result = await _authService.ChangePasswordAsync(request.Email, request.NewPassword);
        if (!result)
            return NotFound(new { message = "User not found" });

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _authService.DeleteUserAsync(id);
        if (!result)
            return NotFound(new { message = "User not found" });

        return Ok(new { message = "User deleted successfully" });
    }
}
