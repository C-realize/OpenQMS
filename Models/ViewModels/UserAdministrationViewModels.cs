using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models.ViewModels;

public class InviteUserInput
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public List<string> SelectedRoles { get; set; } = ["Employee"];

    public List<string> AvailableRoles { get; set; } = new();
}

public class EditUserRolesInput
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public List<string> SelectedRoles { get; set; } = new();

    public List<string> AvailableRoles { get; set; } = new();
}
