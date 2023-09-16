namespace OpenQMS.Models.ViewModels
{
    public class PoliciesChartViewModel
    {
        public PoliciesChartViewModel()
        {
            this.Data = new List<double>();
            this.BackgroundColor = new List<string>();
            this.Fill = false;
        }

        public string? Label { get; set; }
        public List<string> BackgroundColor { get; set; }
        public string? BorderColor { get; set; }
        public bool Fill { get; set; }
        public int BarThickness { get; set; }
        public List<double> Data { get; set; }
    }
}
