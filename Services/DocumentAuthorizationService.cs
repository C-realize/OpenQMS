using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;

namespace OpenQMS.Services;

public enum DocumentPermission
{
    Read,
    Draft,
    Review,
    Approve
}

public class DocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public DocumentAuthorizationService(
        ApplicationDbContext context,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal user,
        int documentId,
        DocumentPermission permission)
    {
        if (user.IsInRole(Constants.AdministratorsRole))
        {
            return true;
        }

        var roles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(user)
            ?? throw new InvalidOperationException("The signed-in user no longer exists."));
        var grants = await _context.DocumentAccessGrants
            .Where(grant => grant.ControlledDocumentId == documentId && roles.Contains(grant.RoleName))
            .ToListAsync();

        return grants.Any(grant => DocumentAccessRules.Allows(grant, permission));
    }
}
