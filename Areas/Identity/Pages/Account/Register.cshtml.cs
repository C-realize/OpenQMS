using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenQMS.Authorization;

namespace OpenQMS.Areas.Identity.Pages.Account;

[Authorize(Roles = Constants.AdministratorsRole)]
public class RegisterModel : PageModel
{
    public IActionResult OnGet()
    {
        return new RedirectResult("/AppUsers/Invite");
    }

    public IActionResult OnPost()
    {
        return new RedirectResult("/AppUsers/Invite");
    }
}
