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

        public class InvoiceStat
        {
            public required string Type { get; set; }
            public decimal TotalAmount { get; set; }
            public double Percentage { get; set; }
            public string Color { get; set; } = "#ccc";
        }

        public (List<InvoiceStat> Costs, List<InvoiceStat> Sales) GetStatistics(List<int> userIds, DateTime startDate, DateTime endDate)
        {

            var invoices = _db.Invoices.Where(
                i => userIds.Contains(i.CompanyId) && 
                ( (i.IssueDate >= startDate && i.IssueDate <= endDate) || 
                (i.IssueDate < startDate && i.Status == InvoiceStatus.Unpaid))).ToList();
            var costGroups = new[]{
            new{
                Type = "Nowe zapłacone",
                Filter = (Func<DBInvoice, bool>)(i => i.Type == InvoiceType.Cost && i.IssueDate >= startDate && i.IssueDate <= endDate && i.Status == InvoiceStatus.Paid)
                },
                new 
                {
                Type = "Nowe niezapłacone",
                Filter = (Func<DBInvoice, bool>)(i => i.Type == InvoiceType.Cost && i.IssueDate >= startDate && i.IssueDate <= endDate && i.Status == InvoiceStatus.Unpaid)
                },
                new 
                {
                Type = "Stare niezapłacone",
                Filter = (Func<DBInvoice, bool>)(i => i.Type == InvoiceType.Cost && i.IssueDate < startDate && i.Status == InvoiceStatus.Unpaid)
                }
            };
            var salesGroups = new[]{
                new {
                Type = "Nowe zapłacone",
                Filter = (Func<DBInvoice, bool>)(i => i.Type == InvoiceType.Sales && i.IssueDate >= startDate && i.IssueDate <= endDate && i.Status == InvoiceStatus.Paid)
                },
                new {
                Type = "Nowe niezapłacone",
                Filter = (Func<DBInvoice, bool>)(i => i.Type == InvoiceType.Sales && i.IssueDate >= startDate && i.IssueDate <= endDate && i.Status == InvoiceStatus.Unpaid)
                },
                new {
                Type = "Stare niezapłacone",
                Filter = (Func<DBInvoice, bool>)(i => i.Type == InvoiceType.Sales && i.IssueDate < startDate && i.Status == InvoiceStatus.Unpaid)
            }
            };
            var costStats = costGroups.Select(g => new InvoiceStat {
                Type = g.Type,
                TotalAmount = invoices.Where(g.Filter).Sum(i => i.TotalGross),
                Percentage = 0 // Will be set below
            }).Where(stat => stat.TotalAmount > 0).ToList();

            var salesStats = salesGroups.Select(g => new InvoiceStat{
                Type = g.Type,
                TotalAmount = invoices.Where(g.Filter).Sum(i => i.TotalGross),
                Percentage = 0
            }).Where(stat => stat.TotalAmount > 0).ToList();

            var totalCost = costStats.Sum(x => x.TotalAmount);
            var totalSales = salesStats.Sum(x => x.TotalAmount);
            foreach (var stat in costStats)
                stat.Percentage = totalCost == 0 ? 0 : (double)(stat.TotalAmount / totalCost) * 100;

            foreach (var stat in salesStats)
                stat.Percentage = totalSales == 0 ? 0 : (double)(stat.TotalAmount / totalSales) * 100;
            // Simple color palette
            string[] colors = [
                "#124708", "#d66d24", "#efc766", "#005f73",
                "#ae2012", "#94d2bd", "#e9d8a6", "#6b705c"
            ]; 
            for (int i = 0; i < costStats.Count; i++) costStats[i].Color = colors[i % colors.Length];
            for (int i = 0; i < salesStats.Count; i++) salesStats[i].Color = colors[i % colors.Length];

            return (costStats, salesStats);
        }

        public string GenerateChartsHtml(int userId, DateTime startDate, DateTime endDate, bool includeHousehold = false)
        {
            List<int> userIds = new List<int> { userId };

            if (includeHousehold)
            {
                // ZMIANA z Users na Employees
                var user = _db.Employees.FirstOrDefault(u => u.Id == userId);
                if (user != null && user.CompanyId.HasValue)
                {
                    userIds = _db.Employees.Where(u => u.CompanyId == user.CompanyId).Select(u => u.Id).ToList();
                }
            }

            var (costs, sales) = GetStatistics(userIds, startDate, endDate);
            var sb = new StringBuilder();

            sb.Append(GetTooltipAssets());
            sb.Append("<div class='charts-container' style='display: flex; flex-wrap: wrap; gap: 20px; justify-content: center; margin-top: 20px;'>");
            sb.Append(GenerateSingleChartHtml(includeHousehold ? "Wydatki (Firma)" : "Wydatki (Moje)", costs));
            sb.Append(GenerateSingleChartHtml(includeHousehold ? "Przychody (Firma)" : "Przychody (Moje)", sales));
            sb.Append("</div>");
            return sb.ToString();
        }

        public string GenerateDashboardChartsHtml(int userId)
        {
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = now.Date.AddDays(1).AddTicks(-1);
            return GenerateChartsHtml(userId, startDate, endDate, false);
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

        private string GenerateSingleChartHtml(string title, List<InvoiceStat> stats)
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
                sb.Append($"<circle cx='100' cy='100' r='100' fill='{stat.Color}' class='pie-slice' data-name='{stat.Type}' data-value='{amountStr}' data-percent='100' />");
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

                    sb.Append($"<path d='{pathData}' fill='{stat.Color}' stroke='white' stroke-width='1' class='pie-slice' data-name='{stat.Type}' data-value='{amountStr}' data-percent='{stat.Percentage:F1}' />");
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
                            <span><strong>{stat.Type}</strong>: {stat.Percentage:F1}%</span>
                        </li>
                ");
            }

            sb.Append("</ul></div></div>");
            return sb.ToString();
        }
    }
}