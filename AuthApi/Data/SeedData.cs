using ApiGeneral.AuthApi.Entities;
using Microsoft.AspNetCore.Identity;

namespace ApiGeneral.AuthApi.Data;

public static class SeedData
{
    public static async Task Initialize(
        IServiceProvider services
    )
    {
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles =
        {
            "Admin",
            "Customer",
            "Scanner",
            "Receptionist"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role)
                );
            }
        }

        await CreateUser(
            userManager,
            "admin@tickets.com",
            "Admin1234!",
            "Admin"
        );

        await CreateUser(
            userManager,
            "customer@tickets.com",
            "Customer1234!",
            "Customer"
        );

        await CreateUser(
            userManager,
            "scanner@tickets.com",
            "Scanner1234!",
            "Scanner"
        );

        await CreateUser(
            userManager,
            "receptionist@tickets.com",
            "Recept1234!",
            "Receptionist"
        );
    }

    private static async Task CreateUser(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role
    )
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user != null)
            return;

        user = new ApplicationUser
        {
            Email = email,
            UserName = role,
            FullName = role
        };

        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, role);
    }
}