using GenZCoders.DTOs.DashBoardsDto;

namespace GenZCoders.Services.AdminDashboardService
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetDashboardAsync();
    }
}