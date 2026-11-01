using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Models
{
    public class PatientDependent
    {
        public int DependentId { get; set; }

        public int PatientId { get; set; }

        [StringLength(150)]
        public string? DependentName { get; set; }

        [StringLength(100)]
        public string? Relationship { get; set; }

        public DateTime? DOB { get; set; }

        public Patient Patient { get; set; } = null!;
    }
}
