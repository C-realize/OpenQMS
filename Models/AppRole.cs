using Microsoft.AspNetCore.Identity;

namespace OpenQMS.Models
{
    public class AppRole : IdentityRole<int>
    {
        public string? Description { get; set; }
    }
}
