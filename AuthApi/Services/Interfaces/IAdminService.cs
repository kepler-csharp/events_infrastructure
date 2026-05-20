using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;


public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
}
