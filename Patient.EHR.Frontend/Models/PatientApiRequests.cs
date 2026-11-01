using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Models
{
    public sealed class PatientUpsertRequest
    {
        [Required]
        public Patient Patient { get; set; } = new();

        public List<PatientDependentRequest>? Dependents { get; set; } = new();
    }

    public sealed class PatientDependentRequest
    {
        [StringLength(150)]
        public string? DependentName { get; set; }

        [StringLength(100)]
        public string? Relationship { get; set; }

        public DateTime? DOB { get; set; }
    }

    public sealed class EncounterSaveRequest
    {
        [Range(1, int.MaxValue)]
        public int PatientId { get; set; }

        [StringLength(2000)]
        public string? ChiefComplaint { get; set; }

        public string? HPI { get; set; }

        public string? PMH { get; set; }

        public string? PlanOfCare { get; set; }

        public List<VitalSignRequest>? VitalSigns { get; set; } = new();
    }

    public sealed class VitalSignRequest
    {
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
    }
}
