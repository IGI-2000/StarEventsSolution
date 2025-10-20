using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        // GET: /Report
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Report/SalesReport?from=2025-01-01&to=2025-01-31
        public async Task<ActionResult> SalesReport(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.UtcNow.Date.AddMonths(-1);
            var t = to ?? DateTime.UtcNow.Date;
            var vm = await _reportService.GetSalesReportAsync(f, t);
            return View(vm);
        }

        // ----------------------------------------------------------------------
        // NEW: Export Sales Report as CSV
        // ----------------------------------------------------------------------
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ExportSalesReport(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.UtcNow.Date.AddMonths(-1);
            var t = to ?? DateTime.UtcNow.Date;
            var vm = await _reportService.GetSalesReportAsync(f, t);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Event,EventDate,TicketsSold,Revenue");
            foreach (var it in vm.Items)
            {
                var date = it.EventDate == DateTime.MinValue ? "" : it.EventDate.ToString("yyyy-MM-dd");
                sb.AppendLine($"\"{it.EventName}\",{date},{it.TicketsSold},{it.Revenue:F2}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"SalesReport_{f:yyyyMMdd}_{t:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
