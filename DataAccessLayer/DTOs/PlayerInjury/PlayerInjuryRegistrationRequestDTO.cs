using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.DTOs.PlayerInjury
{
    // Payload sent by the client when recording a new injury incident.
    // Body part / severity / status are numeric codes that map to the
    // BusinessLayer enums (BodyParts.enBodyPart, Severities.enSeverity,
    // PlayerStatuses.enPlayerStatus).
    public class PlayerInjuryRegistrationRequestDTO
    {
        [Required(ErrorMessage = "Player medical dossier is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid player medical dossier.")]
        public int PlayerMedicalDossierID { get; set; }

        [Required(ErrorMessage = "Body part is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid body part.")]
        public int BodyPart { get; set; }

        [Required(ErrorMessage = "Severity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid severity.")]
        public int Severity { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid status.")]
        public int Status { get; set; }

        [Required(ErrorMessage = "Injury date is required.")]
        [DataType(DataType.Date)]
        public DateTime InjuryDate { get; set; }

        // Optional — an "expected return date" is not always known at intake (TBD).
        [DataType(DataType.Date)]
        public DateTime? EstimatedReturnDate { get; set; }
    }
}
