using DataAccessLayer.DTOs.PlayerMedicalDossier;
using DataAccessLayer.Providers;
using BusinessLayer.Exceptions;

namespace BusinessLayer
{
    public class PlayerMedicalDossier
    {
        public int ID { get; set; }
        public int BloodTypeID { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalNotes { get; set; }
        public int PlayerID { get; set; }

        private PlayerMedicalDossierRegistrationSaveDTO _saveDTO;

        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        public PlayerMedicalDossier(PlayerMedicalDossierRegistrationRequestDTO dto)
        {
            _InitializeFromRequestDTO(dto);
            _InitializeSaveDTO();
            mode = enMode.addNew;
        }

        private void _InitializeFromRequestDTO(PlayerMedicalDossierRegistrationRequestDTO dto)
        {
            this.BloodTypeID = dto.BloodTypeID;
            this.Allergies = dto.Allergies;
            this.MedicalNotes = dto.MedicalNotes;
            this.PlayerID = dto.PlayerID;
        }

        private void _InitializeSaveDTO()
        {
            _saveDTO = new PlayerMedicalDossierRegistrationSaveDTO();
            _saveDTO.BloodTypeID = this.BloodTypeID;
            _saveDTO.Allergies = this.Allergies;
            _saveDTO.MedicalNotes = this.MedicalNotes;
            _saveDTO.PlayerID = this.PlayerID;
        }

        private async Task<bool> _addNew()
        {
            if (! await BloodTypeProvider.DoesBloodTypeExist(this.BloodTypeID))
            {
                throw new BloodTypeNotFoundException(this.BloodTypeID);
            }

            this.ID = PlayerMedicalDossierProvider.AddNew(_saveDTO);

            return this.ID != -1;
        }

        public async Task<bool> Save()
        {
            switch (mode)
            {
                case enMode.addNew:
                    if (await _addNew())
                    {
                        mode = enMode.update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case enMode.update:
                    return false;
            }

            return false;
        }
    }
}
