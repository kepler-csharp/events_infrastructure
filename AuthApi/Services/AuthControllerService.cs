using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.DTOs.AuthDTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Org.BouncyCastle.Math.EC;
using StackExchange.Redis;

namespace ApiGeneral.AuthApi.Services;

public class AuthControllerService : IAuthControllerService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    private readonly IConnectionMultiplexer _redis;
    private readonly IMinioClient _minio;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _email;

    public AuthControllerService(
        UserManager<ApplicationUser> userManager,
        JwtService jwtService,
        IConnectionMultiplexer redis,
        IMinioClient minio,
        IConfiguration configuration,
        IEmailService email
    )
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _redis = redis;
        _minio = minio;
        _configuration = configuration;
        _email = email;
    }

    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return new UnauthorizedObjectResult(
                "Invalid credentials"
            );
        }

        var valid =
            await _userManager.CheckPasswordAsync(
                user,
                dto.Password
            );

        if (!valid)
        {
            return new UnauthorizedObjectResult(
                "Invalid credentials"
            );
        }

        var accessToken =
            await _jwtService.GenerateAccessToken(user);

        var refreshToken =
            _jwtService.GenerateRefreshToken(user.Id);

        return new OkObjectResult(new
        {
            accessToken,
            refreshToken
        });
    }

    public async Task<IActionResult> Logout(string token)
    {
        var db = _redis.GetDatabase();

        var jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

        var expiration =
            jwt.ValidTo - DateTime.UtcNow;

        await db.StringSetAsync(
            $"blacklist:{token}",
            "revoked",
            expiration
        );

        return new OkObjectResult(
            "Logged out successfully"
        );
    }

    public async Task<IActionResult> RegisterAdmin(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Admin");
    }

    public async Task<IActionResult> RegisterCustomer(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Customer");
    }

    public async Task<IActionResult> RegisterScanner(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Scanner");
    }

    public async Task<IActionResult> RegisterReceptionist(
        RegisterDto dto
    )
    {
        return await RegisterUser(dto, "Receptionist");
    }
    
    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> GetProfile(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return new UnauthorizedResult();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        return new OkObjectResult(new UserProfileDto
        {
            FullName = user.FullName ?? string.Empty,
            Email    = user.Email    ?? string.Empty,
            PhotoUrl = user.PhotoUrl
        });
    }
    
    // ── Change Password ───────────────────────────────────────────────────────

    public async Task<IActionResult> ChangePassword(
        ClaimsPrincipal principal,
        ChangePasswordDto dto
    )
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return new UnauthorizedResult();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        return new OkObjectResult(new { message = "Password changed successfully" });
    }
    

    private async Task<IActionResult> RegisterUser(
        RegisterDto dto,
        string role
    )
    {
        var exists =
            await _userManager.FindByEmailAsync(dto.Email);

        if (exists != null)
        {
            return new BadRequestObjectResult(
                "User already exists"
            );
        }

        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.FullName
        };

        var result =
            await _userManager.CreateAsync(
                user,
                dto.Password
            );

        if (!result.Succeeded)
        {
            return new BadRequestObjectResult(
                result.Errors
            );
        }

        await _userManager.AddToRoleAsync(user, role);

        // Send welcome email (fire and forget)
        _ = SendWelcomeEmailAsync(user);

        return new OkObjectResult(new
        {
            message =
                $"{role} registered successfully"
        });
    }
    
    private async Task SendWelcomeEmailAsync(ApplicationUser user)
    {
        try
        {
            await _email.SendWelcomeEmailAsync(
                user.Email!,
                user.FullName ?? user.Email!
            );
        }
        catch
        {
            // Silently ignore — no bloquear el registro
        }
    }

    public async Task<IActionResult> UploadPhoto(
        ClaimsPrincipal principal,
        IFormFile file
    )
    {
        if (file == null || file.Length == 0)
        {
            return new BadRequestObjectResult(
                "No file uploaded"
            );
        }

        const string bucketName = "user-photos";

        var exists =
            await _minio.BucketExistsAsync(
                new BucketExistsArgs()
                    .WithBucket(bucketName)
            );

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket(bucketName)
            );
        }

        var fileName =
            $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        using var stream = file.OpenReadStream();

        await _minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType)
        );

        String? url = _configuration["Minio:EndpointOut"];
        var photoUrl =
            $"http://{url}/{bucketName}/{fileName}";

        var userId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new UnauthorizedResult();
        }

        var user =
            await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new NotFoundObjectResult(
                "User not found"
            );
        }

        user.PhotoUrl = photoUrl;

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return new BadRequestObjectResult(
                result.Errors
            );
        }

        return new OkObjectResult(new
        {
            photoUrl = user.PhotoUrl
        });
    }
}