using System.Text;
using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Seed;
using ApiGeneral.AuthApi.Services;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddScoped<JwtService>();

builder.Services.AddScoped<IAuthControllerService, AuthControllerService>();

// ── Email & QR ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IQrService,    QrService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    );
});

// Domain services
builder.Services.AddScoped<IVenueService,    VenueService>();
builder.Services.AddScoped<IEventService,    EventService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<ISeatService,     SeatService>();
builder.Services.AddScoped<IOrderService,    OrderService>();
builder.Services.AddScoped<IScannerService,  ScannerService>();
builder.Services.AddScoped<IAdminService,    AdminService>();
builder.Services.AddScoped<IReceptionistService, ReceptionistService>();

builder.Services.AddHostedService<ExpiredReservationsCleanupService>();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!
                        )
                    ),

                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token =
                    context.Request.Headers["Authorization"]
                        .ToString()
                        .Replace("Bearer ", "");

                context.HttpContext.Items["Token"] = token;

                return Task.CompletedTask;
            },

            OnTokenValidated = async context =>
            {
                var redis =
                    context.HttpContext.RequestServices
                        .GetRequiredService<IConnectionMultiplexer>();

                var db = redis.GetDatabase();

                var token =
                    context.HttpContext.Items["Token"]
                        ?.ToString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    context.Fail("Missing token");

                    return;
                }

                var isBlacklisted =
                    await db.KeyExistsAsync(
                        $"blacklist:{token}"
                    );

                if (isBlacklisted)
                {
                    context.Fail("Token revoked");
                }
            }
        };
    });

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!
    )
);

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var config =
        builder.Configuration.GetSection("Minio");

    return new MinioClient()
        .WithEndpoint(config["Endpoint"])
        .WithCredentials(
            config["AccessKey"],
            config["SecretKey"]
        )
        .Build();
});

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new()
        {
            Title = "Tickets API",
            Version = "v1"
        }
    );

    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header"
        }
    );

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type =
                            Microsoft.OpenApi.Models.ReferenceType
                                .SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        }
    );
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// seedeer develop
using (var scope = app.Services.CreateScope())
{
    await SeedData.Initialize(scope.ServiceProvider);
}

// seedeer deploy
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//
//     await SeedData.Initialize(services);
// }

app.Run();

