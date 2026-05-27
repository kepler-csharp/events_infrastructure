using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs.AdminDTOs;
using ApiGeneral.AuthApi.DTOs.DashboardDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    private string AdminUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private string AdminEmail =>
        User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    /// <summary>Dashboard: sales, occupancy, revenue (cached 5 min in Redis)</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await _admin.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }

    // ── Customers ─────────────────────────────────────────────────────────────

    /// <summary>Lista todos los clientes (role Customer) paginado.</summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _admin.GetCustomersAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<UserAdminDto>>.Ok(result));
    }

    /// <summary>Detalle de un cliente por ID.</summary>
    [HttpGet("customers/{id}")]
    public async Task<IActionResult> GetCustomer(string id)
    {
        var result = await _admin.GetUserByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Customer not found."));
        return Ok(ApiResponse<UserAdminDto>.Ok(result));
    }

    /// <summary>Actualiza nombre, email e IsActive de un cliente.</summary>
    [HttpPut("customers/{id}")]
    public async Task<IActionResult> UpdateCustomer(string id, [FromBody] UpdateUserRequest req)
    {
        var result = await _admin.UpdateUserAsync(id, req);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Customer not found."));

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "UpdateCustomer", "Customer", id,
                                  newValues: $"FullName={req.FullName},Email={req.Email},IsActive={req.IsActive}");

        return Ok(ApiResponse<UserAdminDto>.Ok(result, "Customer updated."));
    }

    /// <summary>Desactiva un cliente (soft delete).</summary>
    [HttpDelete("customers/{id}")]
    public async Task<IActionResult> DeactivateCustomer(string id)
    {
        var ok = await _admin.DeactivateUserAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Customer not found."));

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "DeactivateCustomer", "Customer", id);

        return Ok(ApiResponse<object>.Ok(null!, "Customer deactivated."));
    }

    /// <summary>Reactiva un cliente previamente desactivado.</summary>
    [HttpPatch("customers/{id}/reactivate")]
    public async Task<IActionResult> ReactivateCustomer(string id)
    {
        var ok = await _admin.ReactivateUserAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Customer not found."));

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "ReactivateCustomer", "Customer", id);

        return Ok(ApiResponse<object>.Ok(null!, "Customer reactivated."));
    }

    // ── Employees (Scanner / Receptionist) ───────────────────────────────────

    /// <summary>Lista todos los empleados (Scanner, Receptionist) paginado.</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await _admin.GetEmployeesAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<UserAdminDto>>.Ok(result));
    }

    /// <summary>Detalle de un empleado por ID.</summary>
    [HttpGet("employees/{id}")]
    public async Task<IActionResult> GetEmployee(string id)
    {
        var result = await _admin.GetEmployeeByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Employee not found."));
        return Ok(ApiResponse<UserAdminDto>.Ok(result));
    }

    /// <summary>Actualiza nombre, email e IsActive de un empleado.</summary>
    [HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateUserRequest req)
    {
        var result = await _admin.UpdateEmployeeAsync(id, req);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Employee not found."));

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "UpdateEmployee", "Employee", id,
                                  newValues: $"FullName={req.FullName},Email={req.Email},IsActive={req.IsActive}");

        return Ok(ApiResponse<UserAdminDto>.Ok(result, "Employee updated."));
    }

    /// <summary>Desactiva un empleado (soft delete).</summary>
    [HttpDelete("employees/{id}")]
    public async Task<IActionResult> DeactivateEmployee(string id)
    {
        var ok = await _admin.DeactivateEmployeeAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Employee not found."));

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "DeactivateEmployee", "Employee", id);

        return Ok(ApiResponse<object>.Ok(null!, "Employee deactivated."));
    }

    /// <summary>Resetea la contraseña de un empleado (sin conocer la anterior).</summary>
    [HttpPatch("employees/{id}/reset-password")]
    public async Task<IActionResult> ResetEmployeePassword(
        string id,
        [FromBody] AdminResetPasswordRequest req
    )
    {
        var ok = await _admin.AdminResetPasswordAsync(id, req.NewPassword);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Employee not found."));

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "ResetEmployeePassword", "Employee", id);

        return Ok(ApiResponse<object>.Ok(null!, "Password reset successfully."));
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    /// <summary>Exporta CSV de ventas por rango de fechas.</summary>
    [HttpGet("reports/export")]
    public async Task<IActionResult> ExportSales(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to
    )
    {
        if (from > to)
            return BadRequest(ApiResponse<object>.Fail("'from' debe ser anterior a 'to'."));

        var csvBytes = await _admin.ExportSalesCsvAsync(from, to);
        var fileName = $"ventas_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";

        _ = _admin.LogAuditAsync(AdminUserId, AdminEmail, "ExportSales", "Report",
                                  entityId: null,
                                  newValues: $"from={from:yyyy-MM-dd},to={to:yyyy-MM-dd}");

        return File(csvBytes, "text/csv", fileName);
    }

    // ── Audit Log ─────────────────────────────────────────────────────────────

    /// <summary>Log de acciones administrativas (quién cambió qué y cuándo).</summary>
    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog([FromQuery] AuditLogFilterRequest filter)
    {
        var result = await _admin.GetAuditLogAsync(filter);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
    }
}
