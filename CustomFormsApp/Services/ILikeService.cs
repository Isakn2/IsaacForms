using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public interface ILikeService
    {
        Task<(bool IsLiked, int LikeCount)> ToggleLikeAsync(int templateId, string userId);
        Task<bool> HasUserLikedAsync(int templateId, string userId);
        Task<int> GetLikeCountAsync(int templateId);
    }
}