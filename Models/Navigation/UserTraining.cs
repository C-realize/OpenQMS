namespace OpenQMS.Models.Navigation
{
    public class UserTraining
    {
        public int TraineeId { get; set; }
        public int TrainingId { get; set; }
        public AppUser Trainee { get; set; }
        public Training Training { get; set; }
    }
}
