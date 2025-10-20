using System;
using System.Threading.Tasks;
using StarEvents.Models.ViewModels;

namespace StarEvents.Services.Interfaces
{
    public interface IReportService
    {
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime from, DateTime to);
    }
}