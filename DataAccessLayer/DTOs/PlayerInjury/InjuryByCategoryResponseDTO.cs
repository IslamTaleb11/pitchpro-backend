using System;

namespace DataAccessLayer.DTOs.PlayerInjury
{
    // Read model for the injury ledger of a squad category.
    // Numeric codes (BodyPart, Severity) come straight from the DB; the *Name
    // fields are resolved from the BusinessLayer enums before the DTO leaves
    // the business layer, so the client gets both the code and a display label.
    public class InjuryByCategoryResponseDTO
    {
        public int ID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerImage { get; set; }
        public string CategoryName { get; set; }

        public int BodyPart { get; set; }
        public string BodyPartName { get; set; }

        public int Severity { get; set; }
        public string SeverityName { get; set; }

        public DateTime InjuryDate { get; set; }
        public DateTime? EstimatedReturnDate { get; set; }
    }
}
