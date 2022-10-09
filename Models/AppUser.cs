using Microsoft.AspNetCore.Identity;
using OpenQMS.Models.Navigation;

namespace OpenQMS.Models
{
    public class AppUser : IdentityUser<int>
    {
        [PersonalData]
        public string FirstName { get; set; }

        [PersonalData]
        public string LastName { get; set; }
        public ICollection<UserTraining> Trainings { get; set; } 

    }
}
