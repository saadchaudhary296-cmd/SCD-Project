using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient.EHR.Data;
using Patient.EHR.Models;
using PatientEntity = Patient.EHR.Models.Patient;

namespace Patient.EHR.Controllers
{
    [ApiController]
    [Route("api/patient")]
    [RequireSession]
    public sealed class PatientApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PatientApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var patients = await _db.Patients
                .AsNoTracking()
                .Where(patient => patient.IsActive)
                .OrderBy(patient => patient.LastName)
                .ThenBy(patient => patient.FirstName)
                .ToListAsync(cancellationToken);

            return Ok(patients);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string term,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Ok(Array.Empty<PatientEntity>());
            }

            var searchTerm = $"%{term.Trim()}%";
            var patients = await _db.Patients
                .AsNoTracking()
                .Where(patient => patient.IsActive && (
                    EF.Functions.Like(patient.FirstName, searchTerm) ||
                    EF.Functions.Like(patient.LastName, searchTerm) ||
                    EF.Functions.Like(patient.MRN, searchTerm)))
                .OrderBy(patient => patient.LastName)
                .ThenBy(patient => patient.FirstName)
                .Take(25)
                .ToListAsync(cancellationToken);

            return Ok(patients);
        }

        [HttpGet("{patientId:int}")]
        public async Task<IActionResult> GetById(
            int patientId,
            CancellationToken cancellationToken)
        {
            var patient = await _db.Patients
                .AsNoTracking()
                .Include(item => item.Dependents)
                .SingleOrDefaultAsync(item => item.PatientId == patientId, cancellationToken);

            if (patient is null)
            {
                return NotFound();
            }

            var dependents = patient.Dependents.Select(dependent => new
            {
                dependent.DependentId,
                dependent.PatientId,
                dependent.DependentName,
                dependent.Relationship,
                dependent.DOB
            }).ToList();

            patient.Dependents = new List<PatientDependent>();
            return Ok(new { Patient = patient, Dependents = dependents });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Create(
            [FromBody] PatientUpsertRequest request,
            CancellationToken cancellationToken)
        {
            if (await _db.Patients.AnyAsync(
                    patient => patient.MRN == request.Patient.MRN,
                    cancellationToken))
            {
                return Conflict(new { message = "A patient with this MRN already exists." });
            }

            var patient = request.Patient;
            patient.PatientId = 0;
            patient.Dependents = ToDependents(request.Dependents ?? new List<PatientDependentRequest>());
            patient.Documents = new List<PatientDocument>();
            patient.Encounters = new List<Encounter>();
            patient.CreatedDate = DateTime.UtcNow;

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { patientId = patient.PatientId }, new
            {
                patient.PatientId,
                message = "Patient saved successfully."
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] PatientUpsertRequest request,
            CancellationToken cancellationToken)
        {
            var patientId = request.Patient.PatientId;
            var existing = await _db.Patients
                .Include(patient => patient.Dependents)
                .SingleOrDefaultAsync(patient => patient.PatientId == patientId, cancellationToken);

            if (existing is null)
            {
                return NotFound();
            }

            if (await _db.Patients.AnyAsync(
                    patient => patient.MRN == request.Patient.MRN && patient.PatientId != patientId,
                    cancellationToken))
            {
                return Conflict(new { message = "A patient with this MRN already exists." });
            }

            var createdDate = existing.CreatedDate;
            _db.Entry(existing).CurrentValues.SetValues(request.Patient);
            existing.CreatedDate = createdDate;
            _db.PatientDependents.RemoveRange(existing.Dependents);
            existing.Dependents = ToDependents(request.Dependents ?? new List<PatientDependentRequest>());

            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("{patientId:int}")]
        public async Task<IActionResult> Delete(
            int patientId,
            CancellationToken cancellationToken)
        {
            var patient = await _db.Patients
                .SingleOrDefaultAsync(
                    item => item.PatientId == patientId && item.IsActive,
                    cancellationToken);

            if (patient is null)
            {
                return NotFound();
            }

            patient.IsActive = false;
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("documents/{patientId:int}")]
        public async Task<IActionResult> GetDocuments(
            int patientId,
            CancellationToken cancellationToken)
        {
            var documents = await _db.PatientDocuments
                .AsNoTracking()
                .Where(document => document.PatientId == patientId)
                .OrderByDescending(document => document.UploadedDate)
                .Select(document => new
                {
                    document.DocumentId,
                    document.DocumentName,
                    document.DocumentType,
                    document.FileName,
                    document.UploadedDate
                })
                .ToListAsync(cancellationToken);

            return Ok(documents);
        }

        [HttpPost("upload")]
        [RequestSizeLimit(21 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] int patientId,
            [FromForm] string documentName,
            [FromForm] string documentType,
            [FromForm] IFormFile file,
            CancellationToken cancellationToken)
        {
            if (patientId <= 0 ||
                string.IsNullOrWhiteSpace(documentName) ||
                string.IsNullOrWhiteSpace(documentType) ||
                file is null ||
                file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Patient, document details, and a file are required." });
            }

            if (documentName.Trim().Length > 200 ||
                documentType.Trim().Length > 100 ||
                Path.GetFileName(file.FileName).Length > 255 ||
                file.ContentType.Length > 150)
            {
                return BadRequest(new { success = false, message = "Document details exceed the allowed length." });
            }

            if (file.Length > 20 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "The maximum document size is 20 MB." });
            }

            if (!await _db.Patients.AnyAsync(
                    patient => patient.PatientId == patientId,
                    cancellationToken))
            {
                return NotFound(new { success = false, message = "Patient was not found." });
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            var document = new PatientDocument
            {
                PatientId = patientId,
                DocumentName = documentName.Trim(),
                DocumentType = documentType.Trim(),
                FileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                FileContent = memoryStream.ToArray()
            };

            _db.PatientDocuments.Add(document);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, document.DocumentId });
        }

        [HttpGet("document/{documentId:int}/download")]
        public async Task<IActionResult> DownloadDocument(
            int documentId,
            CancellationToken cancellationToken)
        {
            var document = await _db.PatientDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

            return document is null
                ? NotFound()
                : File(document.FileContent, document.ContentType, document.FileName);
        }

        private static List<PatientDependent> ToDependents(
            IEnumerable<PatientDependentRequest> dependents)
        {
            return dependents
                .Where(dependent =>
                    !string.IsNullOrWhiteSpace(dependent.DependentName) ||
                    !string.IsNullOrWhiteSpace(dependent.Relationship) ||
                    dependent.DOB.HasValue)
                .Select(dependent => new PatientDependent
                {
                    DependentName = dependent.DependentName?.Trim(),
                    Relationship = dependent.Relationship?.Trim(),
                    DOB = dependent.DOB
                })
                .ToList();
        }
    }

    [ApiController]
    [Route("api/encounter")]
    [RequireSession]
    public sealed class EncounterApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EncounterApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("patient/{patientId:int}")]
        public async Task<IActionResult> GetForPatient(
            int patientId,
            CancellationToken cancellationToken)
        {
            var encounters = await _db.Encounters
                .AsNoTracking()
                .Where(encounter => encounter.PatientId == patientId)
                .OrderByDescending(encounter => encounter.EncounterDate)
                .Select(encounter => new
                {
                    encounter.EncounterId,
                    encounter.PatientId,
                    encounter.ChiefComplaint,
                    encounter.HPI,
                    encounter.PMH,
                    encounter.PlanOfCare,
                    encounter.EncounterDate,
                    VitalSigns = encounter.VitalSigns.Select(vital => new
                    {
                        vital.VitalSignId,
                        vital.BloodPressure,
                        vital.HeartRate,
                        vital.Temperature,
                        vital.RespiratoryRate,
                        vital.SPO2,
                        vital.Weight
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            return Ok(encounters);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save(
            [FromBody] EncounterSaveRequest request,
            CancellationToken cancellationToken)
        {
            if (!await _db.Patients.AnyAsync(
                    patient => patient.PatientId == request.PatientId,
                    cancellationToken))
            {
                return NotFound(new { message = "Patient was not found." });
            }

            var encounter = new Encounter
            {
                PatientId = request.PatientId,
                ChiefComplaint = request.ChiefComplaint,
                HPI = request.HPI,
                PMH = request.PMH,
                PlanOfCare = request.PlanOfCare,
                EncounterDate = DateTime.UtcNow,
                VitalSigns = (request.VitalSigns ?? new List<VitalSignRequest>()).Select(vital => new VitalSign
                {
                    BloodPressure = vital.BloodPressure,
                    HeartRate = vital.HeartRate,
                    Temperature = vital.Temperature,
                    RespiratoryRate = vital.RespiratoryRate,
                    SPO2 = vital.SPO2,
                    Weight = vital.Weight
                }).ToList()
            };

            _db.Encounters.Add(encounter);
            await _db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetForPatient),
                new { patientId = encounter.PatientId },
                new { encounter.EncounterId });
        }
    }
}
