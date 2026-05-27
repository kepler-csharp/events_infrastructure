using ApiGeneral.AuthApi.DTOs.AdminDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IUserManagementService
{
    Task<PagedResult<UserDto>> GetAllUsersAsync(int page, int pageSize, string? role);
    Task<UserDto?>             GetUserByIdAsync(string id);
    Task<UserDto?>             UpdateUserAsync(string id, UpdateUserRequest request);
    Task<bool>                 DeactivateUserAsync(string id);
    Task<bool>                 ActivateUserAsync(string id);
    Task<UserDto>              CreateEmployeeAsync(AdminRegisterRequest request);

    // Employees = Scanner + Receptionist
    Task<PagedResult<UserDto>> GetEmployeesAsync(int page, int pageSize);
    Task<bool>                 DeleteEmployeeAsync(string id);
}
