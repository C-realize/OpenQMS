using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models.ViewModels
{
    public class AuthoredOnGroup
    {
        [DataType(DataType.Date)]
        public DateTime AuthoredOn { get; set; }
        public int DocumentCount { get; set; }
    }
}
