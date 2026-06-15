namespace PharmaSkincare.Services
{
    public interface IFileService
    {
        Task<string?> SaveImageAsync(IFormFile file, string folder = "products");
        Task<string?> SaveDocumentAsync(IFormFile file, string folder = "documents");
        void DeleteFile(string filePath);
    }
}
