using Microsoft.AspNetCore.Identity;
using OpenQMS.Models.Navigation;
using System.ComponentModel.DataAnnotations;

namespace OpenQMS.Models
{
    public class Training
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; }
        public string TrainerId { get; set; }
        public string TrainerEmail { get; set; }
        public ICollection<UserTraining> Trainees { get; set; }
    }
}
