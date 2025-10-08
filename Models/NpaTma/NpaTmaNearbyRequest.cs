namespace Livability.Api.Models.NpaTma
{
    public class NpaTmaNearbyRequest
    {
        public int year { get; set; }
        public int month { get; set; }
        public decimal lat { get; set; }
        public decimal lon { get; set; }
        public decimal radiusKm { get; set; } = 30;
    }
}
