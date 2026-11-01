using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Models
{
    public class VitalSign
    {
        public int VitalSignId { get; set; }

        public int EncounterId { get; set; }

        [StringLength(30)]
        public string? BloodPressure { get; set; }

        [StringLength(30)]
        public string? HeartRate { get; set; }

        [StringLength(30)]
        public string? Temperature { get; set; }

        [StringLength(30)]
        public string? RespiratoryRate { get; set; }

        [StringLength(30)]
        public string? SPO2 { get; set; }

        [StringLength(30)]
        public string? Weight { get; set; }

        public Encounter Encounter { get; set; } = null!;
    }
}
