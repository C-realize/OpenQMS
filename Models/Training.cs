using OpenQMS.Models.Navigation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenQMS.Models
{
    public class Training
    {
        public int Id { get; set; }
        public string TrainingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; }
        public string TrainerId { get; set; }
        public string TrainerEmail { get; set; }
        public ICollection<UserTraining> Trainees { get; set; }
        [Display(Name = "Completed By")]
        public int? CompletedBy { get; set; }
        [ForeignKey("CompletedBy")]
        public virtual AppUser? CompletedByUser { get; set; }
        [Display(Name = "Completed On")]
        public DateTime? CompletedOn { get; set; }
        public TrainingStatus Status { get; set; }
        public enum TrainingStatus
        {
            Scheduled,
            Completed
        }
    }
}
