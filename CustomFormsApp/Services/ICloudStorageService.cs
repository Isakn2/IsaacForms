namespace CustomFormsApp.Services;

public interface ICloudStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task DeleteFileAsync(string fileName);
}