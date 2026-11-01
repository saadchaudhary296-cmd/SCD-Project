using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Models
{
    public class PatientDocument
    {
        public int DocumentId { get; set; }

        public int PatientId { get; set; }

        [Required]
        [StringLength(200)]
        public string DocumentName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ContentType { get; set; } = "application/octet-stream";

        [Required]
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        public Patient Patient { get; set; } = null!;
    }
}
