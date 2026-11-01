using Microsoft.EntityFrameworkCore;
using Patient.EHR.Models;
using PatientEntity = Patient.EHR.Models.Patient;

namespace Patient.EHR.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PatientEntity> Patients => Set<PatientEntity>();

        public DbSet<PatientDependent> PatientDependents => Set<PatientDependent>();

        public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();

        public DbSet<Encounter> Encounters => Set<Encounter>();

        public DbSet<VitalSign> VitalSigns => Set<VitalSign>();

        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasKey(user => user.UserId);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<PatientDependent>()
                .HasKey(dependent => dependent.DependentId);

            modelBuilder.Entity<PatientDocument>()
                .HasKey(document => document.DocumentId);

            modelBuilder.Entity<PatientEntity>()
                .HasKey(patient => patient.PatientId);

            modelBuilder.Entity<PatientEntity>()
                .HasIndex(patient => patient.MRN)
                .IsUnique();

            modelBuilder.Entity<PatientEntity>()
                .Property(patient => patient.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<PatientEntity>()
                .HasMany(patient => patient.Dependents)
                .WithOne(dependent => dependent.Patient)
                .HasForeignKey(dependent => dependent.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientEntity>()
                .HasMany(patient => patient.Documents)
                .WithOne(document => document.Patient)
                .HasForeignKey(document => document.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientEntity>()
                .HasMany(patient => patient.Encounters)
                .WithOne(encounter => encounter.Patient)
                .HasForeignKey(encounter => encounter.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Encounter>()
                .HasMany(encounter => encounter.VitalSigns)
                .WithOne(vitalSign => vitalSign.Encounter)
                .HasForeignKey(vitalSign => vitalSign.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientDocument>()
                .Property(document => document.FileContent)
                .HasColumnType("varbinary(max)");
        }
    }
}