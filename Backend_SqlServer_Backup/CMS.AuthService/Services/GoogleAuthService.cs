using CMS.AuthService.Data;
using CMS.AuthService.DTOs;
using CMS.AuthService.Models;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;

namespace CMS.AuthService.Services;

public interface IGoogleAuthService
{
    Task<AuthResponse?> GoogleLoginAsync(string code);
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        AuthDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<GoogleAuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse?> GoogleLoginAsync(string idToken)
    {
        try
        {
            // Validate Google ID token
            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Google ClientId not configured");
                return null;
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });

            if (payload == null)
            {
                _logger.LogWarning("Invalid Google token");
                return null;
            }

            _logger.LogInformation("Google login attempt for email: {Email}", payload.Email);

            // Check if user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject || u.Email == payload.Email);

            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Email = payload.Email,
                    FirstName = payload.GivenName ?? "User",
                    LastName = payload.FamilyName ?? "",
                    GoogleId = payload.Subject,
                    ProfilePictureUrl = payload.Picture,
                    AuthProvider = AuthProvider.Google,
                    Role = UserRole.Student, // Default role
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user created via Google: {Email}", user.Email);
            }
            else
            {
                // Update Google info if needed
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = payload.Subject;
                }
                if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    user.ProfilePictureUrl = payload.Picture;
                }
                user.AuthProvider = AuthProvider.Google;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Existing user logged in via Google: {Email}", user.Email);
            }

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                PhotoUrl = user.PhotoUrl ?? user.ProfilePictureUrl,
                UserId = user.UserId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return null;
        }
    }
}
