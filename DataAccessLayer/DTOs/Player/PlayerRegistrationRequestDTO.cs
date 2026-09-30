using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataAccessLayer.DTOs.Player
{
    public class PlayerRegistrationRequestDTO
    {
        // ── 01. Identity & Access (→ persons table) ──────────────────────────
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(255, MinimumLength = 2)]
        public string FirstName { get; set; }

        [StringLength(255)]
        public string? SecondName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(255, MinimumLength = 2)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public bool Gender { get; set; } // true = Male, false = Female

        [Required(ErrorMessage = "Birth date is required.")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Club ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Club.")]
        public int ClubID { get; set; }

        // ── 02. Technical Profile (→ players table) ──────────────────────────
        [Required(ErrorMessage = "Photo is required.")]
        public IFormFile Photo { get; set; }

        [Required(ErrorMessage = "Primary position is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Primary Position.")]
        public int PrimaryPositionID { get; set; }

        // Optional — not required
        public int? SecondaryPositionID { get; set; }

        [Required(ErrorMessage = "Preferred foot is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Preferred Foot.")]
        public int PreferredFootID { get; set; }

        [Required(ErrorMessage = "Jersey number is required.")]
        [StringLength(10)]
        public string JerseyNumber { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(1000, MinimumLength = 5)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Category.")]
        public int CategoryID { get; set; }

        // ── 03. Medical Dossier (→ players_medical_dossiers table) ───────────
        [Required(ErrorMessage = "Blood type is required.")]
        [Range(1, 8, ErrorMessage = "Please select a valid Blood Type.")]
        public int BloodTypeID { get; set; }

        // Optional — not required
        [StringLength(1000)]
        public string? Allergies { get; set; }

        // Optional — not required
        [StringLength(2000)]
        public string? MedicalNotes { get; set; }

        // ── 04. Adult Details (→ adult_players_details) ───────────────────────
        [StringLength(255)]
        public string? Phone { get; set; }

        [StringLength(1000)]
        [EmailAddress]
        public string? Email { get; set; }

        // ── 05. Minor Details (→ minor_players_details) ───────────────────────
        [StringLength(500)]
        public string? GuardianFullName { get; set; }

        [StringLength(255)]
        public string? GuardianPhone { get; set; }
    }
}
