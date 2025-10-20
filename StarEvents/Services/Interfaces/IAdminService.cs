using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarEvents.Models.ViewModels;

namespace StarEvents.Services.Interfaces
{
    public interface IAdminService
    {
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime from, DateTime to);
        Task<List<UserViewModel>> GetUsersAsync();
        Task<List<DiscountViewModel>> GetDiscountsAsync();
        Task<DiscountViewModel> GetDiscountAsync(int id);
        Task<(bool Success, string Error)> CreateOrUpdateDiscountAsync(DiscountViewModel vm);
        Task<bool> DeleteDiscountAsync(int id);

        // New: events list for admin UI
        Task<List<EventAdminViewModel>> GetEventsForAdminAsync();
    }
}