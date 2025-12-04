using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT15.Data;
using IT15.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15.Services
{
    public class FinancialAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public FinancialAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FinancialSnapshot> GetSnapshotAsync(DateTime referenceDateUtc)
        {
            var dayStart = referenceDateUtc.Date;
            var dayEnd = dayStart.AddDays(1);
            var monthStart = new DateTime(referenceDateUtc.Year, referenceDateUtc.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var dailyIncome = await _context.CompanyLedger.AsNoTracking()
                .Where(l => l.EntryType == LedgerEntryType.Income &&
                            l.TransactionDate >= dayStart && l.TransactionDate < dayEnd)
                .SumAsync(l => l.Amount);

            var dailyExpense = await _context.CompanyLedger.AsNoTracking()
                .Where(l => l.EntryType == LedgerEntryType.Expense &&
                            l.TransactionDate >= dayStart && l.TransactionDate < dayEnd)
                .SumAsync(l => -l.Amount);

            var monthlyIncome = await _context.CompanyLedger.AsNoTracking()
                .Where(l => l.EntryType == LedgerEntryType.Income &&
                            l.TransactionDate >= monthStart && l.TransactionDate < monthEnd)
                .SumAsync(l => l.Amount);

            var monthlyExpense = await _context.CompanyLedger.AsNoTracking()
                .Where(l => l.EntryType == LedgerEntryType.Expense &&
                            l.TransactionDate >= monthStart && l.TransactionDate < monthEnd)
                .SumAsync(l => -l.Amount);

            var monthlyByCategory = await _context.CompanyLedger.AsNoTracking()
                .Where(l => l.TransactionDate >= monthStart && l.TransactionDate < monthEnd)
                .GroupBy(l => new { l.Category, l.EntryType })
                .Select(g => new
                {
                    g.Key.Category,
                    g.Key.EntryType,
                    Total = g.Key.EntryType == LedgerEntryType.Expense ? g.Sum(x => -x.Amount) : g.Sum(x => x.Amount)
                })
                .ToListAsync();

            decimal GetCategoryTotal(LedgerEntryCategory category, LedgerEntryType type) =>
                monthlyByCategory
                    .Where(x => x.Category == category && x.EntryType == type)
                    .Select(x => x.Total)
                    .FirstOrDefault();

            return new FinancialSnapshot
            {
                DailyIncome = dailyIncome,
                DailyExpenses = dailyExpense,
                MonthlyIncome = monthlyIncome,
                MonthlyExpenses = monthlyExpense,
                MonthlyPayrollExpenses = GetCategoryTotal(LedgerEntryCategory.Payroll, LedgerEntryType.Expense),
                MonthlySupplyExpenses = GetCategoryTotal(LedgerEntryCategory.Supplies, LedgerEntryType.Expense),
                MonthlyOperationalExpenses = GetCategoryTotal(LedgerEntryCategory.Operations, LedgerEntryType.Expense),
                MonthlySalesIncome = GetCategoryTotal(LedgerEntryCategory.Sales, LedgerEntryType.Income)
            };
        }

        public async Task<ReceivablesSummary> GetReceivablesSummaryAsync(DateTime referenceDateUtc)
        {
            var receivables = await _context.AccountsReceivables
                .Include(r => r.LedgerEntries)
                .AsNoTracking()
                .ToListAsync();

            var summary = new ReceivablesSummary();

            foreach (var receivable in receivables)
            {
                var collected = receivable.LedgerEntries
                    .Where(e => e.EntryType == LedgerEntryType.Income)
                    .Sum(e => e.Amount);

                var outstanding = receivable.InvoiceAmount - collected;

                summary.TotalInvoices++;
                summary.TotalInvoiced += receivable.InvoiceAmount;
                summary.TotalCollected += collected;
                summary.TotalOutstanding += Math.Max(0, outstanding);

                if (outstanding > 0m)
                {
                    if (receivable.DueDate.HasValue && receivable.DueDate.Value.Date < referenceDateUtc.Date)
                    {
                        summary.OverdueOutstanding += outstanding;
                        summary.OverdueCount++;
                    }
                    else
                    {
                        summary.PendingCount++;
                    }
                }
            }

            return summary;
        }

        public async Task<PayablesSummary> GetPayablesSummaryAsync(DateTime referenceDateUtc)
        {
            var payables = await _context.AccountsPayables
                .Include(p => p.LedgerEntries)
                .AsNoTracking()
                .ToListAsync();

            var summary = new PayablesSummary();

            foreach (var payable in payables)
            {
                var paid = payable.LedgerEntries
                    .Where(e => e.EntryType == LedgerEntryType.Expense)
                    .Sum(e => -e.Amount);

                var outstanding = payable.BillAmount - paid;

                summary.TotalBills++;
                summary.TotalBilled += payable.BillAmount;
                summary.TotalPaid += paid;
                summary.TotalOutstanding += Math.Max(0, outstanding);

                if (outstanding > 0m)
                {
                    if (payable.DueDate.HasValue && payable.DueDate.Value.Date < referenceDateUtc.Date)
                    {
                        summary.OverdueOutstanding += outstanding;
                        summary.OverdueCount++;
                    }
                    else
                    {
                        summary.PendingCount++;
                    }
                }
            }

            return summary;
        }

        public async Task<RevenueAnalysis> GetRevenueAnalysisAsync(int monthsBack = 12)
        {
            var cutoff = DateTime.UtcNow.Date.AddMonths(-monthsBack + 1);

            // Fetch aggregates in SQL and materialize before creating the record to avoid translation issues.
            var monthlySalesRaw = await _context.CompanyLedger.AsNoTracking()
                .Where(l => l.EntryType == LedgerEntryType.Income
                            && l.Category == LedgerEntryCategory.Sales
                            && l.TransactionDate >= cutoff)
                .GroupBy(l => new { l.TransactionDate.Year, l.TransactionDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.Amount)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var monthlySales = monthlySalesRaw
                .Select(g => new MonthlyRevenuePoint(new DateTime(g.Year, g.Month, 1), g.Total))
                .ToList();

            if (monthlySales.Count < 4)
            {
                return new RevenueAnalysis
                {
                    MonthlyRevenues = monthlySales,
                    HasEnoughData = false
                };
            }

            var ordered = monthlySales.OrderByDescending(p => p.Total).ToList();
            var groupSize = Math.Max(2, ordered.Count / 4);

            var highMonths = ordered.Take(groupSize).ToList();
            var lowMonths = ordered.Skip(Math.Max(0, ordered.Count - groupSize)).Take(groupSize).ToList();

            var highMetrics = CalculateDistribution(highMonths);
            var lowMetrics = CalculateDistribution(lowMonths);

            var tStatistic = ComputeWelchT(highMetrics.Mean, highMetrics.Variance, highMetrics.Count,
                                           lowMetrics.Mean, lowMetrics.Variance, lowMetrics.Count);

            var degreesOfFreedom = ComputeWelchDegreesOfFreedom(highMetrics.Variance, highMetrics.Count,
                                                                lowMetrics.Variance, lowMetrics.Count);

            var pValue = ComputeTwoTailedPValue(tStatistic, degreesOfFreedom);

            return new RevenueAnalysis
            {
                MonthlyRevenues = monthlySales,
                HighMonths = highMonths,
                LowMonths = lowMonths,
                HighMean = highMetrics.Mean,
                LowMean = lowMetrics.Mean,
                HighStandardDeviation = Math.Sqrt(highMetrics.Variance),
                LowStandardDeviation = Math.Sqrt(lowMetrics.Variance),
                WelchTStatistic = tStatistic,
                DegreesOfFreedom = degreesOfFreedom,
                PValue = pValue,
                HasEnoughData = true
            };
        }

        private static DistributionMetrics CalculateDistribution(List<MonthlyRevenuePoint> points)
        {
            var count = points.Count;
            var mean = points.Average(p => (double)p.Total);
            var variance = count > 1
                ? points.Sum(p => Math.Pow((double)p.Total - mean, 2)) / (count - 1)
                : 0d;

            return new DistributionMetrics(count, mean, variance);
        }

        private static double ComputeWelchT(double mean1, double var1, int n1, double mean2, double var2, int n2)
        {
            var numerator = mean1 - mean2;
            var denominator = Math.Sqrt((var1 / n1) + (var2 / n2));
            return denominator == 0 ? 0 : numerator / denominator;
        }

        private static double ComputeWelchDegreesOfFreedom(double var1, int n1, double var2, int n2)
        {
            var s1 = var1 / n1;
            var s2 = var2 / n2;
            var numerator = Math.Pow(s1 + s2, 2);
            var denominator = (Math.Pow(s1, 2) / (n1 - 1)) + (Math.Pow(s2, 2) / (n2 - 1));
            return denominator == 0 ? double.PositiveInfinity : numerator / denominator;
        }

        private static double ComputeTwoTailedPValue(double t, double degreesOfFreedom)
        {
            if (double.IsNaN(t) || double.IsNaN(degreesOfFreedom) || degreesOfFreedom <= 0)
            {
                return double.NaN;
            }

            var absT = Math.Abs(t);
            var cdf = StudentTCdf(absT, degreesOfFreedom);
            return double.IsNaN(cdf) ? double.NaN : Math.Max(0, Math.Min(1, 2 * (1 - cdf)));
        }

        private static double StudentTCdf(double t, double v)
        {
            if (t < 0)
            {
                return 1 - StudentTCdf(-t, v);
            }

            double pdf(double x)
            {
                var part = LogGamma((v + 1) / 2) - LogGamma(v / 2) - 0.5 * Math.Log(v * Math.PI);
                var exponent = -((v + 1) / 2) * Math.Log(1 + (x * x) / v);
                return Math.Exp(part + exponent);
            }

            double IntegrateSimpson(double upper)
            {
                if (upper == 0)
                {
                    return 0;
                }

                var intervals = Math.Max(200, (int)(upper * 40));
                if (intervals % 2 == 1)
                {
                    intervals++;
                }

                var h = upper / intervals;
                var sum = pdf(0) + pdf(upper);

                for (var i = 1; i < intervals; i++)
                {
                    var x = i * h;
                    sum += (i % 2 == 0 ? 2 : 4) * pdf(x);
                }

                return sum * h / 3;
            }

            var integral = IntegrateSimpson(t);
            return 0.5 + integral;
        }

        private static double LogGamma(double x)
        {
            var coefficients = new[]
            {
                676.5203681218851,
                -1259.1392167224028,
                771.32342877765313,
                -176.61502916214059,
                12.507343278686905,
                -0.13857109526572012,
                9.9843695780195716e-6,
                1.5056327351493116e-7
            };

            if (x < 0.5)
            {
                return Math.Log(Math.PI) - Math.Log(Math.Sin(Math.PI * x)) - LogGamma(1 - x);
            }

            x -= 1;
            var a = 0.99999999999980993;
            for (var i = 0; i < coefficients.Length; i++)
            {
                a += coefficients[i] / (x + i + 1);
            }

            var t = x + coefficients.Length - 0.5;
            return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(t) - t + Math.Log(a);
        }

        private record DistributionMetrics(int Count, double Mean, double Variance);
    }

    public class FinancialSnapshot
    {
        public decimal DailyIncome { get; set; }
        public decimal DailyExpenses { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal MonthlySalesIncome { get; set; }
        public decimal MonthlyPayrollExpenses { get; set; }
        public decimal MonthlySupplyExpenses { get; set; }
        public decimal MonthlyOperationalExpenses { get; set; }
        public decimal MonthlyNetIncome => MonthlyIncome - MonthlyExpenses;
        public decimal DailyNetIncome => DailyIncome - DailyExpenses;
    }

    public class ReceivablesSummary
    {
        public int TotalInvoices { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueOutstanding { get; set; }
    }

    public class PayablesSummary
    {
        public int TotalBills { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueOutstanding { get; set; }
    }

    public class RevenueAnalysis
    {
        public List<MonthlyRevenuePoint> MonthlyRevenues { get; set; } = new();
        public List<MonthlyRevenuePoint> HighMonths { get; set; } = new();
        public List<MonthlyRevenuePoint> LowMonths { get; set; } = new();
        public double HighMean { get; set; }
        public double LowMean { get; set; }
        public double HighStandardDeviation { get; set; }
        public double LowStandardDeviation { get; set; }
        public double WelchTStatistic { get; set; }
        public double DegreesOfFreedom { get; set; }
        public double PValue { get; set; }
        public bool HasEnoughData { get; set; }
    }

    public record MonthlyRevenuePoint(DateTime Month, decimal Total);
}
