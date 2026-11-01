using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Models
{
    public class Encounter
    {
        public int EncounterId { get; set; }

        public int PatientId { get; set; }

        [StringLength(2000)]
        public string? ChiefComplaint { get; set; }

        public string? HPI { get; set; }

        public string? PMH { get; set; }

        public string? PlanOfCare { get; set; }

        public DateTime EncounterDate { get; set; } = DateTime.UtcNow;

        public Patient Patient { get; set; } = null!;

        public ICollection<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();
    }
}
