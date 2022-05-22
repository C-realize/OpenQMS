namespace OpenQMS.Models.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
