using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenQMS.Models;
using OpenQMS.Services;

namespace OpenQMS.Areas.Identity.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting("email-sign-in")]
public class LoginModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<LoginModel> _logger;
    private readonly EmailOptions _emailOptions;

    public LoginModel(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<LoginModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string ReturnUrl { get; private set; } = "/";

    public string? SignInEmail { get; private set; }

    public string? SignInToken { get; private set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null && !await _userManager.IsLockedOutAsync(user))
        {
            if (!user.EmailConfirmed)
            {
                await SendConfirmationLinkAsync(user);
            }
            else
            {
                var token = await _userManager.GenerateUserTokenAsync(
                    user,
                    TokenOptions.DefaultProvider,
                    "EmailSignIn");
                var publicUri = GetPublicBaseUri();
                var link = Url.Page(
                    "./Login",
                    pageHandler: "Verify",
                    values: new { email = user.Email, token, returnUrl = ReturnUrl },
                    protocol: publicUri.Scheme,
                    host: publicUri.Authority);

                if (string.IsNullOrWhiteSpace(link))
                {
                    throw new InvalidOperationException("The sign-in link could not be generated.");
                }

                var safeLink = HtmlEncoder.Default.Encode(link);
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Your OpenQMS sign-in link",
                    $"<p>Use this link to sign in to OpenQMS. It expires in 15 minutes and can be used once:</p><p><a href=\"{safeLink}\">Sign in</a></p>");
                _logger.LogInformation("A sign-in link was sent to a confirmed account.");
            }
        }

        StatusMessage = "If the address belongs to an active account, a sign-in link will arrive shortly.";
        return RedirectToPage("./Login");
    }

    public async Task<IActionResult> OnGetVerifyAsync(string? email, string? token, string? returnUrl = null)
    {
        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await _userManager.FindByEmailAsync(email);

        if (user is null
            || !user.EmailConfirmed
            || await _userManager.IsLockedOutAsync(user)
            || string.IsNullOrWhiteSpace(token)
            || !await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                "EmailSignIn",
                token))
        {
            StatusMessage = "The sign-in link is invalid, expired, or already used. Request a new link.";
            return RedirectToPage("./Login");
        }

        SignInEmail = user.Email;
        SignInToken = token;
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostVerifyAsync(string? email, string? token, string? returnUrl = null)
    {
        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await _userManager.FindByEmailAsync(email);

        if (user is null
            || !user.EmailConfirmed
            || await _userManager.IsLockedOutAsync(user)
            || string.IsNullOrWhiteSpace(token)
            || !await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                "EmailSignIn",
                token))
        {
            StatusMessage = "The sign-in link is invalid, expired, or already used. Request a new link.";
            return RedirectToPage("./Login");
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException("The sign-in link could not be consumed: "
                + string.Join("; ", stampResult.Errors.Select(error => error.Description)));
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/"));
    }

    private async Task SendConfirmationLinkAsync(AppUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var publicUri = GetPublicBaseUri();
        var confirmationLink = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, code = encodedToken },
            protocol: publicUri.Scheme,
            host: publicUri.Authority);
        if (string.IsNullOrWhiteSpace(confirmationLink))
        {
            throw new InvalidOperationException("The account confirmation link could not be generated.");
        }

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your OpenQMS account",
            $"<p>Confirm your email address to activate your OpenQMS account:</p><p><a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">Confirm email</a></p>");
    }

    private Uri GetPublicBaseUri()
    {
        return Uri.TryCreate(_emailOptions.PublicBaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri
            : new Uri($"{Request.Scheme}://{Request.Host}");
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
