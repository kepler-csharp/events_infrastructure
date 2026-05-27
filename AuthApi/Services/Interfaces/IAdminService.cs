using ApiGeneral.AuthApi.DTOs.AdminDTOs;
using ApiGeneral.AuthApi.DTOs.DashboardDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IAdminService
{
    // ── Dashboard ─────────────────────────────────────────────────────────────
    Task<DashboardDto> GetDashboardAsync();

    // ── All Users (Customers) ─────────────────────────────────────────────────
    Task<PagedResult<UserAdminDto>> GetCustomersAsync(int page, int pageSize);
    Task<UserAdminDto?>             GetUserByIdAsync(string id);
    Task<UserAdminDto?>             UpdateUserAsync(string id, UpdateUserRequest req);
    Task<bool>                      DeactivateUserAsync(string id);
    Task<bool>                      ReactivateUserAsync(string id);

    // ── Employees (non-Admin, non-Customer roles) ─────────────────────────────
    Task<PagedResult<UserAdminDto>> GetEmployeesAsync(int page, int pageSize);
    Task<UserAdminDto?>             GetEmployeeByIdAsync(string id);
    Task<UserAdminDto?>             UpdateEmployeeAsync(string id, UpdateUserRequest req);
    Task<bool>                      DeactivateEmployeeAsync(string id);
    Task<bool>                      AdminResetPasswordAsync(string id, string newPassword);

    // ── Reports ───────────────────────────────────────────────────────────────
    /// <summary>Exporta CSV de ventas por rango de fechas.</summary>
    Task<byte[]> ExportSalesCsvAsync(DateTime from, DateTime to);

    // ── Audit Log ─────────────────────────────────────────────────────────────
    Task<PagedResult<AuditLogDto>> GetAuditLogAsync(AuditLogFilterRequest filter);

    /// <summary>Registra una acción administrativa en el log de auditoría.</summary>
    Task LogAuditAsync(string adminUserId, string adminEmail, string action,
                       string entityType, string? entityId = null,
                       string? oldValues = null, string? newValues = null);
}
