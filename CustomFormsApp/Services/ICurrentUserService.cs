namespace CustomFormsApp.Services;

public interface ICurrentUserService
{
    string? GetUserId();
    string? GetUserEmail();
    string? GetUsername();
    bool IsAuthenticated();
    bool IsAdmin(); // Add this method signature
}