using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;

namespace ApiGeneral.AuthApi.Services;

public class AuthControllerService : IAuthControllerService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    private readonly IConnectionMultiplexer _redis;
    private readonly IMinioClient _minio;

    public AuthControllerService(
        UserManager<ApplicationUser> userManager,
        JwtService jwtService,
        IConnectionMultiplexer redis,
        IMinioClient minio
    )
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _redis = redis;
        _minio = minio;
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

        return new OkObjectResult(new
        {
            message =
                $"{role} registered successfully"
        });
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

        var photoUrl =
            $"http://localhost:9000/{bucketName}/{fileName}";

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