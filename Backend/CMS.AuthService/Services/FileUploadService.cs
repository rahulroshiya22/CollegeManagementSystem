namespace CMS.AuthService.Services;

public interface IFileUploadService
{
    Task<string?> UploadPhotoAsync(IFormFile file, int userId, string userType);
    bool DeletePhoto(string photoUrl);
}

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string?> UploadPhotoAsync(IFormFile file, int userId, string userType)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for upload");
                return null;
            }

            if (file.Length > MaxFileSize)
            {
                _logger.LogWarning("File size exceeds maximum allowed size: {Size}", file.Length);
                return null;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Invalid file extension: {Extension}", extension);
                return null;
            }

            // Create directory structure
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles", userType.ToLower());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return relative URL
            var photoUrl = $"/uploads/profiles/{userType.ToLower()}/{fileName}";
            _logger.LogInformation("Photo uploaded successfully: {PhotoUrl}", photoUrl);
            
            return photoUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return null;
        }
    }

    public bool DeletePhoto(string photoUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(photoUrl))
                return false;

            var filePath = Path.Combine(_environment.WebRootPath, photoUrl.TrimStart('/'));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Photo deleted: {PhotoUrl}", photoUrl);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: {PhotoUrl}", photoUrl);
            return false;
        }
    }
}
