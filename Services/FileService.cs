namespace PharmaSkincare.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedDocTypes = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveImageAsync(IFormFile file, string folder = "products")
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaxFileSizeBytes) throw new InvalidOperationException("File size exceeds 5MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageTypes.Contains(ext)) throw new InvalidOperationException("Invalid image type.");

            return await SaveFileAsync(file, folder, ext);
        }

        public async Task<string?> SaveDocumentAsync(IFormFile file, string folder = "documents")
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaxFileSizeBytes) throw new InvalidOperationException("File size exceeds 5MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedDocTypes.Contains(ext)) throw new InvalidOperationException("Invalid document type.");

            return await SaveFileAsync(file, folder, ext);
        }

        private async Task<string?> SaveFileAsync(IFormFile file, string folder, string ext)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{folder}/{fileName}";
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
