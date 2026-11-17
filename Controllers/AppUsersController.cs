/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Models.ViewModels;
using OpenQMS.Services;

namespace OpenQMS.Controllers
{
    [Authorize(Roles = Constants.AdministratorsRole + "," + Constants.ManagersRole)]
    public class AppUsersController : Controller
    {
        private static readonly string[] StarterRoleNames =
        [
            Constants.AdministratorsRole,
            Constants.ManagersRole,
            "Process Owner / Approver",
            "Training Coordinator",
            "Employee",
            "Auditor"
        ];

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly EmailOptions _emailOptions;
        public AppUsersController(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOptions<EmailOptions> emailOptions)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _emailOptions = emailOptions.Value;
        }

        // GET: UsersController
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (AppUser user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.UserName = user.FirstName + " " + user.LastName;
                thisViewModel.Email = user.Email;
                thisViewModel.Roles = await _userManager.GetRolesAsync(user);
                thisViewModel.EmailConfirmed = user.EmailConfirmed;
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }

        [HttpGet]
        [Authorize(Roles = Constants.AdministratorsRole)]
        public async Task<IActionResult> Invite()
        {
            return View(new InviteUserInput
            {
                AvailableRoles = await GetRoleNamesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Constants.AdministratorsRole)]
        public async Task<IActionResult> Invite(InviteUserInput input)
        {
            input.AvailableRoles = await GetRoleNamesAsync();
            input.SelectedRoles ??= new List<string>();
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var email = input.Email.Trim();
            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                ModelState.AddModelError(nameof(input.Email), "An account already uses this email address.");
                return View(input);
            }

            if (input.SelectedRoles is null
                || input.SelectedRoles.Count == 0
                || input.SelectedRoles.Except(input.AvailableRoles, StringComparer.Ordinal).Any())
            {
                ModelState.AddModelError(nameof(input.SelectedRoles), "Select one or more valid roles.");
                return View(input);
            }

            var user = new AppUser
            {
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                UserName = email,
                Email = email,
                EmailConfirmed = false
            };
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                return View(input);
            }

            var roleResult = await _userManager.AddToRolesAsync(user, input.SelectedRoles);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                return View(input);
            }

            _context.QmsAuditEvents.Add(new QmsAuditEvent
            {
                ActorUserId = GetActorId(),
                Action = "UserInvited",
                EntityType = nameof(AppUser),
                EntityId = user.Id.ToString(),
                Details = string.Join(", ", input.SelectedRoles)
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await SendConfirmationLinkAsync(user);
            TempData["StatusMessage"] = $"Invitation sent to {email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Constants.AdministratorsRole)]
        public async Task<IActionResult> ResendInvitation(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
            {
                return NotFound();
            }

            if (!user.EmailConfirmed)
            {
                await SendConfirmationLinkAsync(user);
            }

            TempData["StatusMessage"] = user.EmailConfirmed
                ? "This account has already confirmed its email."
                : $"Invitation sent to {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Constants.AdministratorsRole)]
        public async Task<IActionResult> EditRoles(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
            {
                return NotFound();
            }

            return View(new EditUserRolesInput
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList(),
                AvailableRoles = await GetRoleNamesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Constants.AdministratorsRole)]
        public async Task<IActionResult> EditRoles(EditUserRolesInput input)
        {
            var user = await _userManager.FindByIdAsync(input.UserId.ToString());
            if (user is null)
            {
                return NotFound();
            }

            input.AvailableRoles = await GetRoleNamesAsync();
            input.SelectedRoles ??= new List<string>();
            if (input.SelectedRoles is null
                || input.SelectedRoles.Except(input.AvailableRoles, StringComparer.Ordinal).Any())
            {
                return BadRequest("The role list contains an unknown role.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Contains(Constants.AdministratorsRole)
                && !input.SelectedRoles.Contains(Constants.AdministratorsRole)
                && (await _userManager.GetUsersInRoleAsync(Constants.AdministratorsRole)).Count <= 1)
            {
                ModelState.AddModelError(nameof(input.SelectedRoles), "The last system administrator role cannot be removed.");
                input.Email = user.Email ?? string.Empty;
                return View(input);
            }

            var removedRoles = existingRoles.Except(input.SelectedRoles, StringComparer.Ordinal).ToArray();
            var addedRoles = input.SelectedRoles.Except(existingRoles, StringComparer.Ordinal).ToArray();
            if (removedRoles.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, removedRoles);
                if (!removeResult.Succeeded)
                {
                    AddIdentityErrors(removeResult);
                    input.Email = user.Email ?? string.Empty;
                    return View(input);
                }
            }
            if (addedRoles.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, addedRoles);
                if (!addResult.Succeeded)
                {
                    AddIdentityErrors(addResult);
                    input.Email = user.Email ?? string.Empty;
                    return View(input);
                }
            }

            _context.QmsAuditEvents.Add(new QmsAuditEvent
            {
                ActorUserId = GetActorId(),
                Action = "UserRolesChanged",
                EntityType = nameof(AppUser),
                EntityId = user.Id.ToString(),
                Details = $"Roles: {string.Join(", ", input.SelectedRoles)}"
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<string>> GetRoleNamesAsync()
        {
            return await _roleManager.Roles
                .Where(role => role.Name != null && StarterRoleNames.Contains(role.Name))
                .OrderBy(role => role.Name)
                .Select(role => role.Name!)
                .ToListAsync();
        }

        private async Task SendConfirmationLinkAsync(AppUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var publicUri = Uri.TryCreate(_emailOptions.PublicBaseUrl, UriKind.Absolute, out var configuredUri)
                ? configuredUri
                : new Uri($"{Request.Scheme}://{Request.Host}");
            var confirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = encodedToken },
                protocol: publicUri.Scheme,
                host: publicUri.Authority);
            if (string.IsNullOrWhiteSpace(confirmationUrl))
            {
                throw new InvalidOperationException("The account confirmation link could not be generated.");
            }

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Confirm your OpenQMS account",
                $"<p>Confirm your email address to activate your OpenQMS account:</p><p><a href=\"{HtmlEncoder.Default.Encode(confirmationUrl)}\">Confirm email</a></p>");
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private int GetActorId()
        {
            return int.TryParse(_userManager.GetUserId(User), out var actorId)
                ? actorId
                : throw new InvalidOperationException("The signed-in user ID is invalid.");
        }
    }
}
