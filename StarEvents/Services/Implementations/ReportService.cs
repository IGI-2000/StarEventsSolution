using System;
using System.Threading.Tasks;
using StarEvents.Services.Interfaces;
using StarEvents.Models.ViewModels;

namespace StarEvents.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IAdminService _adminService;

        public ReportService(IAdminService adminService)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        }

        public Task<SalesReportViewModel> GetSalesReportAsync(DateTime from, DateTime to)
        {
            // Delegate to admin service which already builds the sales report
            return _adminService.GetSalesReportAsync(from, to);
        }
    }
}