using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.Role;
using DataAccessLayer.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Role
    {
        public int ID { get; set; }
        public string Name { get; set; }

        private RoleRegistrationSaveDTO _roleRegistrationSaveDTO;
        public RoleRegistrationResponseDTO roleRegistrationResponseDTO = new RoleRegistrationResponseDTO();
        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode;

        private void _InitializeFromRegistrationRequestDTO(RoleRegistrationRequestDTO dto)
        {
            this.Name = dto.Name;
        }

        private void _InitializeRoleSaveDTO()
        {
            _roleRegistrationSaveDTO.Name = this.Name;
        }

        public Role(RoleRegistrationRequestDTO roleRegistrationRequestDTO)
        {
            _InitializeFromRegistrationRequestDTO(roleRegistrationRequestDTO);
            _InitializeRoleSaveDTO();
            mode = enMode.addNew;
        }


        private bool _addNew()
        {
            this.ID = RoleProvider.AddNew(_roleRegistrationSaveDTO, roleRegistrationResponseDTO);

            return this.ID != -1;
        }

        public bool Save()
        {
            switch (mode)
            {
                case enMode.addNew:
                    if (_addNew())
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


        public static async Task<string?> GetRoleName(int roleID)
        {
            return await RoleProvider.GetRoleNameByID(roleID) ?? null;
        }
    }
}
