using System;
using System.Linq;
using System.Threading.Tasks;
using IT15.Data;
using IT15.Models;
using IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SupplyRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;

        public SupplyRequestController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string statusFilter)
        {
            ViewData["StatusFilter"] = statusFilter;

            var requestsQuery = _context.SupplyRequests
                .Include(r => r.Supply)
                    .ThenInclude(s => s.Supplier)
                .Include(r => r.RequestingEmployee)
                .Include(r => r.DeliveryService)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<SupplyRequestStatus>(statusFilter, out var status))
            {
                requestsQuery = requestsQuery.Where(r => r.Status == status);
            }

            var requests = await requestsQuery
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.SupplyRequests
                .Include(r => r.Supply)
                    .ThenInclude(s => s.Supplier)
                .Include(r => r.DeliveryService)
                .Include(r => r.RequestingEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.Status != SupplyRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Supply request could not be approved. It may have already been processed.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Supply == null)
            {
                TempData["ErrorMessage"] = "Supply record for this request is missing.";
                return RedirectToAction(nameof(Index));
            }

            var approver = await _userManager.GetUserAsync(User);
            var deliveryService = request.DeliveryService;

            request.Supply.StockLevel += request.Quantity;
            request.Status = SupplyRequestStatus.Approved;

            var deliveryFee = deliveryService?.Fee ?? 0m;
            var supplyCost = request.TotalCost - deliveryFee;
            if (supplyCost < 0)
            {
                supplyCost = 0;
            }

            if (supplyCost > 0)
            {
                _context.CompanyLedger.Add(new CompanyLedger
                {
                    UserId = request.RequestingEmployeeId,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Supply Cost: {request.Quantity} x {request.Supply.Name}",
                    EntryType = LedgerEntryType.Expense,
                    Category = LedgerEntryCategory.Supplies,
                    ReferenceNumber = $"PO-{request.Id:D6}",
                    Counterparty = request.Supply.Supplier?.Name ?? "Unknown supplier",
                    Amount = -supplyCost
                });
            }

            if (deliveryFee > 0 && deliveryService != null)
            {
                var supplierName = request.Supply.Supplier?.Name ?? "supplier";
                _context.CompanyLedger.Add(new CompanyLedger
                {
                    UserId = request.RequestingEmployeeId,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Delivery Fee: {deliveryService.Name} from {supplierName} for {request.Supply.Name}",
                    EntryType = LedgerEntryType.Expense,
                    Category = LedgerEntryCategory.Operations,
                    ReferenceNumber = $"DF-{request.Id:D6}",
                    Counterparty = deliveryService.Name,
                    Amount = -deliveryFee
                });
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                approver.Id,
                approver.UserName,
                "Supply Request Approved",
                $"Admin '{approver.UserName}' approved supply request #{request.Id} for '{request.Supply.Name}' ({request.Quantity}).");

            TempData["SuccessMessage"] = "Supply request approved and inventory updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var request = await _context.SupplyRequests
                .Include(r => r.RequestingEmployee)
                .Include(r => r.Supply)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.Status != SupplyRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Supply request could not be denied. It may have already been processed.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = SupplyRequestStatus.Denied;
            await _context.SaveChangesAsync();

            var approver = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                approver.Id,
                approver.UserName,
                "Supply Request Denied",
                $"Admin '{approver.UserName}' denied supply request #{request.Id} for '{request.Supply?.Name ?? request.SupplyId.ToString()}'.");

            TempData["SuccessMessage"] = "Supply request has been denied.";
            return RedirectToAction(nameof(Index));
        }
    }
}
