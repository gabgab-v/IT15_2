using IT15.Data;
using IT15.Models;
using System;
using System.Threading.Tasks;

namespace IT15.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string userName, string actionType, string details)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                ActionType = actionType,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}

