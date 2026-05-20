using ApiGeneral.AuthApi.DTOs.DashboardDTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;


public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
}
