namespace VisitorLog_PBFD.Models
{
    public class Report
    {
        public required string PersonName { get; set; }
        public string? ContinentName { get; set; }
        public string? CountryName { get; set; }
        public string? StateName { get; set; }
        public string? CountyName { get; set; }
        public string? CityName { get; set; }
    }

}
