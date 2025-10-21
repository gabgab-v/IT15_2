using System.ComponentModel.DataAnnotations;

namespace IT15.Models
{
    public enum LedgerEntryType
    {
        Income = 0,
        Expense = 1
    }

    public enum LedgerEntryCategory
    {
        Sales = 0,
        Payroll = 1,
        Supplies = 2,
        Operations = 3,
        Other = 4
    }

    public enum ReceivableStatus
    {
        Pending = 0,
        PartiallyCollected = 1,
        Collected = 2,
        Overdue = 3,
        WrittenOff = 4
    }

    public enum PayableStatus
    {
        Pending = 0,
        PartiallyPaid = 1,
        Paid = 2,
        Overdue = 3,
        Cancelled = 4
    }
}
