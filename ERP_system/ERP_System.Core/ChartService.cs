using ERP_System.Core.DBTables;
using ERP_System.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ERP_System.Core
{
    /// <summary>
    /// Service for generating financial statistics and chart data based on Invoices.
    /// Transactions (FinancialOperations) are no longer used.
    /// </summary>
    public class ChartService
    {
        private readonly AppDbContext _db;

        public ChartService(AppDbContext db)
        {
            _db = db;
        }

        public class CategoryStat
        {
            public required string CategoryName { get; set; }
            public decimal TotalAmount { get; set; }
            public double Percentage { get; set; }
            public string Color { get; set; } = "#ccc";
        }

        // Internal DTO to simplify grouping of different data sources
        private class RawStatEntry
        {
            public string Name { get; set; } = "";
            public decimal Amount { get; set; }
            public InvoiceType Type { get; set; }
        }

        /// <summary>
        /// Calculates expenses and incomes statistics for a company within a date range.
        /// Uses Invoices and projected Recurring Invoices.
        /// </summary>
        public (List<CategoryStat> Expenses, List<CategoryStat> Incomes) GetStatistics(List<int> companyIds, DateTime startDate, DateTime endDate)
        {
            var statsData = new List<RawStatEntry>();

            // 1. Fetch Real Invoices
            var invoices = _db.Invoices
                .Include(i => i.Contractor)
                .Where(i => companyIds.Contains(i.CompanyId) && i.IssueDate >= startDate && i.IssueDate <= endDate)
                .ToList();

            foreach (var inv in invoices)
            {
                statsData.Add(new RawStatEntry
                {
                    Name = inv.Contractor?.Name ?? "Nieznany Kontrahent",
                    Amount = inv.TotalGross,
                    Type = inv.Type
                });
            }

            // 2. Fetch Recurring Invoices (Pattern based on FinancialOperations for now)
            var recurringRules = _db.RecurringOperations
                .Include(rt => rt.Invoice)
                .Where(rt => rt.Invoice != null && companyIds.Contains(rt.Invoice.CompanyId) && rt.IsActive)
                .ToList();

            foreach (var rule in recurringRules)
            {
                var currentDate = rule.NextRunDate;
                var unit = (TransactionIntervalType)rule.IntervalType;

                while (currentDate <= endDate)
                {
                    if (currentDate >= startDate)
                    {
                        statsData.Add(new RawStatEntry
                        {
                            Name = (rule.Invoice!.Title ?? "Cykliczna") + " (Plan)",
                            Amount = Math.Abs(rule.Invoice.Value),
                            Type = rule.Invoice.TransactionType == TransactionType.expense ? InvoiceType.Cost : InvoiceType.Sales
                        });
                    }

                    // Move to next occurrence
                    currentDate = unit switch
                    {
                        TransactionIntervalType.Days => currentDate.AddDays(rule.IntervalValue),
                        TransactionIntervalType.Weeks => currentDate.AddDays(rule.IntervalValue * 7),
                        TransactionIntervalType.Months => currentDate.AddMonths(rule.IntervalValue),
                        TransactionIntervalType.Years => currentDate.AddYears(rule.IntervalValue),
                        _ => currentDate.AddMonths(1)
                    };
                }
            }

            // 3. Process Expenses
            var rawExpenses = statsData
                .Where(s => s.Type == InvoiceType.Cost)
                .GroupBy(s => s.Name)
                .Select(g => new { Name = g.Key, Total = g.Sum(s => s.Amount) })
                .ToList();

            var totalExpense = rawExpenses.Sum(x => x.Total);

            // 4. Process Incomes
            var rawIncomes = statsData
                .Where(s => s.Type == InvoiceType.Sales)
                .GroupBy(s => s.Name)
                .Select(g => new { Name = g.Key, Total = g.Sum(s => s.Amount) })
                .ToList();

            var totalIncome = rawIncomes.Sum(x => x.Total);

            // 5. Build CategoryStat results
            var expenseStats = rawExpenses.Select(x => new CategoryStat
            {
                CategoryName = x.Name,
                TotalAmount = x.Total,
                Percentage = totalExpense == 0 ? 0 : (double)(x.Total / totalExpense) * 100
            }).OrderByDescending(x => x.Percentage).ToList();

            var incomeStats = rawIncomes.Select(x => new CategoryStat
            {
                CategoryName = x.Name,
                TotalAmount = x.Total,
                Percentage = totalIncome == 0 ? 0 : (double)(x.Total / totalIncome) * 100
            }).OrderByDescending(x => x.Percentage).ToList();

            // Simple color palette
            string[] colors = [
                "#124708", "#d66d24", "#efc766", "#005f73",
                "#ae2012", "#94d2bd", "#e9d8a6", "#6b705c"
            ];

            for (int i = 0; i < expenseStats.Count; i++) expenseStats[i].Color = colors[i % colors.Length];
            for (int i = 0; i < incomeStats.Count; i++) incomeStats[i].Color = colors[i % colors.Length];

            return (expenseStats, incomeStats);
        }

        public string GenerateDashboardChartsHtml(int userId)
        {
            var user = _db.Employees.FirstOrDefault(u => u.Id == userId);
            if (user == null || !user.CompanyId.HasValue) return "Brak danych (nie należysz do firmy).";

            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = now.Date.AddDays(1).AddTicks(-1);

            return GenerateChartsHtml(userId, startDate, endDate, true);
        }

        public string GenerateChartsHtml(int userId, DateTime startDate, DateTime endDate, bool includeCompany = false)
        {
            List<int> ids = new List<int> { userId };

            if (includeCompany)
            {
                var user = _db.Employees.FirstOrDefault(u => u.Id == userId);
                if (user != null && user.CompanyId.HasValue)
                {
                    ids = new List<int> { user.CompanyId.Value };
                }
            }

            var (expenses, incomes) = GetStatistics(ids, startDate, endDate);
            var sb = new StringBuilder();

            sb.Append(GetTooltipAssets());
            sb.Append("<div class='charts-container' style='display: flex; flex-wrap: wrap; gap: 20px; justify-content: center; margin-top: 20px;'>");
            sb.Append(GenerateSingleChartHtml(includeCompany ? "Wydatki (Firma)" : "Wydatki", expenses));
            sb.Append(GenerateSingleChartHtml(includeCompany ? "Przychody (Firma)" : "Przychody", incomes));
            sb.Append("</div>");
            return sb.ToString();
        }

        private string GetTooltipAssets()
        {
            return """
            <style>
                .chart-tooltip {
                    position: fixed;
                    background: rgba(0, 0, 0, 0.8);
                    color: white;
                    padding: 5px 10px;
                    border-radius: 4px;
                    pointer-events: none;
                    font-size: 0.9em;
                    z-index: 1000;
                    display: none;
                    white-space: nowrap;
                }
                .pie-slice:hover {
                    opacity: 0.8;
                    cursor: pointer;
                }
            </style>
            <script>
                document.addEventListener('mousemove', function(e) {
                    var tooltip = document.getElementById('chart-tooltip');
                    if (!tooltip) return;
                    
                    if (e.target.classList.contains('pie-slice')) {
                        var name = e.target.getAttribute('data-name');
                        var val = e.target.getAttribute('data-value');
                        var pct = e.target.getAttribute('data-percent');
                        
                        tooltip.innerHTML = `<strong>${name}</strong><br>${pct}%<br>${val}`;
                        tooltip.style.display = 'block';
                        tooltip.style.left = (e.clientX + 10) + 'px';
                        tooltip.style.top = (e.clientY + 10) + 'px';
                    } else {
                        tooltip.style.display = 'none';
                    }
                });
            </script>
            <div id="chart-tooltip" class="chart-tooltip"></div>
            """;
        }

        private string GenerateSingleChartHtml(string title, List<CategoryStat> stats)
        {
            if (stats.Count == 0)
            {
                return $@"
                    <div class='chart-box' style='flex: 1; min-width: 300px; max-width: 500px; text-align: center; border: 1px solid #e0e0e0; padding: 20px; border-radius: 12px; background-color: #fff;'>
                        <h3 style='margin-top: 0; color: #333;'>{title}</h3>
                        <p style='color: #666;'>Brak danych w wybranym okresie.</p>
                    </div>";
            }

            var sb = new StringBuilder();
            sb.Append($@"
            <div class='chart-box' style='flex: 1; min-width: 300px; max-width: 500px; border: 1px solid #e0e0e0; padding: 20px; border-radius: 12px; background-color: #fff; box-shadow: 0 2px 4px rgba(0,0,0,0.05);'>
                <h3 style='text-align: center; margin-top: 0; margin-bottom: 20px; color: #333;'>{title}</h3>
                <div class='pie-chart-wrapper' style='display: flex; align-items: center; justify-content: center; flex-wrap: wrap; gap: 20px;'>
            ");

            double currentAngle = -90;
            double radius = 100;
            double centerX = 100;
            double centerY = 100;
            var culture = new CultureInfo("pl-PL");

            sb.Append($"<svg width='200' height='200' viewBox='0 0 200 200' style='transform: rotate(0deg);'>");

            if (stats.Count == 1)
            {
                var stat = stats[0];
                string amountStr = stat.TotalAmount.ToString("C2", culture);
                sb.Append($"<circle cx='100' cy='100' r='100' fill='{stat.Color}' class='pie-slice' data-name='{stat.CategoryName}' data-value='{amountStr}' data-percent='100' />");
            }
            else
            {
                foreach (var stat in stats)
                {
                    double sliceAngle = (stat.Percentage / 100.0) * 360.0;
                    double x1 = centerX + radius * Math.Cos(currentAngle * Math.PI / 180.0);
                    double y1 = centerY + radius * Math.Sin(currentAngle * Math.PI / 180.0);

                    double endAngle = currentAngle + sliceAngle;
                    double x2 = centerX + radius * Math.Cos(endAngle * Math.PI / 180.0);
                    double y2 = centerY + radius * Math.Sin(endAngle * Math.PI / 180.0);

                    int largeArcFlag = sliceAngle > 180 ? 1 : 0;

                    string pathData = $"M {x1.ToString(CultureInfo.InvariantCulture)} {y1.ToString(CultureInfo.InvariantCulture)} A {radius} {radius} 0 {largeArcFlag} 1 {x2.ToString(CultureInfo.InvariantCulture)} {y2.ToString(CultureInfo.InvariantCulture)} L {centerX} {centerY} Z";
                    string amountStr = stat.TotalAmount.ToString("C2", culture);

                    sb.Append($"<path d='{pathData}' fill='{stat.Color}' stroke='white' stroke-width='1' class='pie-slice' data-name='{stat.CategoryName}' data-value='{amountStr}' data-percent='{stat.Percentage:F1}' />");
                    currentAngle += sliceAngle;
                }
            }
            sb.Append("</svg>");

            sb.Append(@"
                    <ul class='chart-legend' style='list-style: none; padding: 0; margin: 0; font-size: 0.9em; max-width: 200px;'>
            ");

            foreach (var stat in stats)
            {
                sb.Append($@"
                        <li style='margin-bottom: 8px; display: flex; align-items: center; color: #555;'>
                            <span style='display: inline-block; width: 14px; height: 14px; background-color: {stat.Color}; margin-right: 10px; border-radius: 3px; flex-shrink: 0;'></span>
                            <span><strong>{stat.CategoryName}</strong>: {stat.Percentage:F1}%</span>
                        </li>
                ");
            }

            sb.Append("</ul></div></div>");
            return sb.ToString();
        }
    }
}
