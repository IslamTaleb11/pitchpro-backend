using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.StaffMedicalDossier;
using DataAccessLayer.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class StaffMedicalDossier
    {
        public int ID { get; set; }
        public int BloodTypeID { get; set; }
        public string Allergies { get; set; }
        public string MedicalNotes { get; set; }
        private StaffMedicalDossierRegistrationSaveDTO _saveDTO;
        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        public StaffMedicalDossier(StaffMedicalDossierRegistrationRequestDTO staffMedicalDossierRegistrationRequestDTO)
        {
            _InitializeFromRegistrationRequestDTO(staffMedicalDossierRegistrationRequestDTO);
            _InitializePersonSaveDTO();
            mode = enMode.addNew;
        }

        private void _InitializeFromRegistrationRequestDTO(StaffMedicalDossierRegistrationRequestDTO dto)
        {
            this.BloodTypeID = dto.BloodTypeID;
            this.Allergies = dto.Allergies;
            this.MedicalNotes = dto.MedicalNotes;
        }


        private void _InitializePersonSaveDTO()
        {
            _saveDTO = new StaffMedicalDossierRegistrationSaveDTO();
            _saveDTO.BloodTypeID = this.BloodTypeID;
            _saveDTO.Allergies = this.Allergies;
            _saveDTO.MedicalNotes = this.MedicalNotes;
        }
        private async Task<bool> _addNew()
        {
            if (! await BloodTypeProvider.DoesBloodTypeExist(this.BloodTypeID))
            {
                throw new BloodTypeNotFoundException(this.BloodTypeID);
            }
            this.ID = StaffMedicalDossierProvider.AddNew(_saveDTO);

            return this.ID != -1;
        }

        private bool _update()
        {
            return false;
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
