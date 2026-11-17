using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Models;

namespace OpenQMS.Data;

public static class SeedData
{
    private static readonly (string Name, string Description)[] StarterRoles =
    [
        (Constants.AdministratorsRole, "Full system configuration and access administration."),
        (Constants.ManagersRole, "Document control and quality-system administration; review role grants before use."),
        ("Process Owner / Approver", "May be granted document approval authority after local review."),
        ("Training Coordinator", "May be granted training administration authority after local review."),
        ("Employee", "May read assigned effective documents and complete assigned training."),
        ("Auditor", "Read-only access when explicitly granted.")
    ];

    private static readonly (string Id, string Title)[] StarterDocuments =
    [
        ("MAPA-01", "Przewodnik i lista dokumentów"),
        ("KJ-01", "Księga jakości, polityka i zakres systemu"),
        ("SOP-01", "Nadzór nad dokumentami, zapisami i zmianami"),
        ("SOP-02", "Kwalifikacja dostawców i komponentów"),
        ("SOP-03", "Kompatybilność PRP i zarządzanie ryzykiem"),
        ("SOP-04", "Własny magazyn, środowisko i przyjęcie dostaw"),
        ("SOP-05", "Kompletacja, pakowanie, weryfikacja i walidacja"),
        ("SOP-06", "Etykiety, UDI, EUDAMED i zwolnienie partii"),
        ("SOP-07", "Identyfikowalność, dystrybucja i wycofanie"),
        ("SOP-08", "Reklamacje, incydenty, niezgodności i CAPA"),
        ("SOP-09", "Kompetencje, audyty i przegląd zarządzania"),
        ("SOP-10", "Nadzór regulacyjny, informacje i reklama"),
        ("DT-PRP-01", "Dokumentacja zestawu PRP KIT"),
        ("OSW-22-01", "Oświadczenie podmiotu zestawiającego"),
        ("FORM-A", "Formularze dostaw, komponentów i magazynu"),
        ("FORM-B", "Formularze kompatybilności, ryzyka i walidacji"),
        ("FORM-C", "Formularze partii, etykiet, UDI i zwolnienia"),
        ("FORM-D", "Formularze bezpieczeństwa i zarządzania jakością")
    ];

    public static async Task Initialize(IServiceProvider serviceProvider, string? bootstrapAdminEmail)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var role in StarterRoles)
        {
            if (await roleManager.FindByNameAsync(role.Name) is null)
            {
                var result = await roleManager.CreateAsync(new AppRole
                {
                    Name = role.Name,
                    Description = role.Description
                });
                EnsureSucceeded(result, $"creating role '{role.Name}'");
            }
        }

        await RemoveLegacyRoleAsync(roleManager, userManager, context, "Administrator");
        await RemoveLegacyRoleAsync(roleManager, userManager, context, "Manager");

        if (!string.IsNullOrWhiteSpace(bootstrapAdminEmail))
        {
            var admin = await userManager.FindByEmailAsync(bootstrapAdminEmail);
            if (admin is null)
            {
                admin = new AppUser
                {
                    FirstName = "System",
                    LastName = "Administrator",
                    UserName = bootstrapAdminEmail,
                    Email = bootstrapAdminEmail,
                    EmailConfirmed = false
                };
                var createResult = await userManager.CreateAsync(admin);
                EnsureSucceeded(createResult, "creating the configured bootstrap administrator");
            }

            if (!await userManager.IsInRoleAsync(admin, Constants.AdministratorsRole))
            {
                var roleResult = await userManager.AddToRoleAsync(admin, Constants.AdministratorsRole);
                EnsureSucceeded(roleResult, "assigning the configured bootstrap administrator role");
            }
        }

        else if (serviceProvider.GetRequiredService<IWebHostEnvironment>().IsProduction()
            && (await userManager.GetUsersInRoleAsync(Constants.AdministratorsRole)).Count == 0)
        {
            throw new InvalidOperationException("Configure BootstrapAdmin:Email for initial administrator setup, or ensure an administrator already exists.");
        }

        var existingDocumentIds = await context.ControlledDocuments
            .Select(document => document.DocumentId)
            .ToListAsync();
        foreach (var (id, title) in StarterDocuments)
        {
            if (!existingDocumentIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            {
                context.ControlledDocuments.Add(new ControlledDocument
                {
                    DocumentId = id,
                    Title = title
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
        }
    }

    private static async Task RemoveLegacyRoleAsync(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        ApplicationDbContext context,
        string roleName)
    {
        var oldRole = await roleManager.FindByNameAsync(roleName);
        if (oldRole is null)
        {
            return;
        }

        var users = await userManager.GetUsersInRoleAsync(roleName);
        foreach (var user in users)
        {
            var result = await userManager.RemoveFromRoleAsync(user, roleName);
            EnsureSucceeded(result, $"removing legacy administrator access from user {user.Id}");
            context.QmsAuditEvents.Add(new QmsAuditEvent
            {
                Action = "LegacyRoleRemoved",
                EntityType = nameof(AppUser),
                EntityId = user.Id.ToString(),
                Details = roleName
            });
        }

        var deleteResult = await roleManager.DeleteAsync(oldRole);
        EnsureSucceeded(deleteResult, $"removing legacy role '{roleName}'");
        await context.SaveChangesAsync();
    }
}
