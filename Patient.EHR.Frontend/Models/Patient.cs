using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Models
{
    public class Patient
    {
        public int PatientId { get; set; }

        [Required]
        [StringLength(50)]
        public string MRN { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime DOB { get; set; }

        [StringLength(30)]
        public string? Gender { get; set; }

        [StringLength(11)]
        public string? SSN { get; set; }

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [StringLength(30)]
        public string? MaritalStatus { get; set; }

        [StringLength(100)]
        public string? Nationality { get; set; }

        [Required]
        [Phone]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        public string? AlternateNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        public string? EmergencyContact { get; set; }

        [StringLength(100)]
        public string? EmergencyRelationship { get; set; }

        [StringLength(255)]
        public string? StreetAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(30)]
        public string? AdmissionType { get; set; }

        [StringLength(100)]
        public string? Ward { get; set; }

        [StringLength(50)]
        public string? Room { get; set; }

        [StringLength(150)]
        public string? ReferringPhysician { get; set; }

        [StringLength(150)]
        public string? InsuranceProvider { get; set; }

        [StringLength(100)]
        public string? PolicyNumber { get; set; }

        [StringLength(30)]
        public string? InsuranceType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public ICollection<PatientDependent> Dependents { get; set; } = new List<PatientDependent>();

        public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();

        public ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
    }
}
