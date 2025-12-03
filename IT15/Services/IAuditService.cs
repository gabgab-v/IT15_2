using System.Threading.Tasks;

namespace IT15.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userName, string actionType, string details);
    }
}

