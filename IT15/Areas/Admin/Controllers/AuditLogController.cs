using IT15.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string actionType, DateTime? startDate, DateTime? endDate)
        {
            ViewData["Search"] = search;
            ViewData["ActionType"] = actionType;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            const int maxRows = 200;

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    EF.Functions.Like(a.UserName ?? string.Empty, $"%{term}%") ||
                    EF.Functions.Like(a.ActionType ?? string.Empty, $"%{term}%") ||
                    EF.Functions.Like(a.Details ?? string.Empty, $"%{term}%"));
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(a => a.ActionType == actionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(a => a.Timestamp < end);
            }

            var auditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(maxRows) // Limit to the most recent logs for performance
                .ToListAsync();

            var actionTypes = await _context.AuditLogs
                .Select(a => a.ActionType)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            ViewData["ActionTypes"] = actionTypes;
            ViewData["MaxRows"] = maxRows;

            return View(auditLogs);
        }
    }
}

