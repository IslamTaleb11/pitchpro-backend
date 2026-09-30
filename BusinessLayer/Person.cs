using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Person
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string? SecondName { get; set; }
        public string LastName { get; set; }
        public bool Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public int ClubID { get; set; }



        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode { get; set; }

        private PersonRegistrationSaveDTO _personRegistrationSaveDTO { get; set; }
        private ClubSaveUpdateDTO _clubSaveUpdateDTO { get; set; }

        public PersonRegistrationResponseDTO personRegistrationResponseDTO = new PersonRegistrationResponseDTO();
        public ClubUpdateResponseDTO clubUpdateResponseDTO = new ClubUpdateResponseDTO();

        private void _InitializeFromRegistrationRequestDTO(PersonRegistrationRequestDTO dto)
        {
            this.FirstName = dto.FirstName;
            this.SecondName = dto.SecondName;
            this.LastName = dto.LastName;
            this.Gender = dto.Gender;
            this.BirthDate = dto.BirthDate;
            this.ClubID = dto.ClubID;
        }

        private void _InitializePersonSaveDTO()
        {
            _personRegistrationSaveDTO = new PersonRegistrationSaveDTO();
            _personRegistrationSaveDTO.FirstName = this.FirstName;
            _personRegistrationSaveDTO.SecondName = this.SecondName;
            _personRegistrationSaveDTO.LastName = this.LastName;
            _personRegistrationSaveDTO.Gender = this.Gender;
            _personRegistrationSaveDTO.BirthDate = this.BirthDate;
            _personRegistrationSaveDTO.ClubID = this.ClubID;
        }

        public Person(PersonRegistrationRequestDTO personRegistrationRequestDTO)
        {
            _InitializeFromRegistrationRequestDTO(personRegistrationRequestDTO);
            _InitializePersonSaveDTO();
            mode = enMode.addNew;
        }

        //private void _InitializeClubSaveUpdateDTO()
        //{
        //    _clubSaveUpdateDTO = new ClubSaveUpdateDTO();
        //    _clubSaveUpdateDTO.ID = this.ID;
        //    _clubSaveUpdateDTO.Name = this.Name;
        //    _clubSaveUpdateDTO.Crest = this.Crest;
        //    _clubSaveUpdateDTO.PrimaryIdentityColor = this.PrimaryIdentityColor;
        //    _clubSaveUpdateDTO.ContactNumber = this.ContactNumber;
        //}

        //private void _InitializeFromUpdateRequestDTO(ClubUpdateRequestDTO dto)
        //{
        //    this.ID = dto.ID;
        //    this.Name = dto.Name;
        //    this.Crest = dto.Crest;
        //    this.PrimaryIdentityColor = dto.PrimaryIdentityColor;
        //    this.ContactNumber = dto.ContactNumber;
        //}

        //public Club(ClubUpdateRequestDTO clubUpdateRequestDTO)
        //{
        //    _InitializeFromUpdateRequestDTO(clubUpdateRequestDTO);
        //    _InitializeClubSaveUpdateDTO();
        //    mode = enMode.update;
        //}

        private bool _addNew()
        {
            if (!ClubProvider.DoesClubExist(_personRegistrationSaveDTO.ClubID))
            {
                throw new ClubNotFoundException(_personRegistrationSaveDTO.ClubID);
            }
            this.ID = PersonProvider.AddNew(_personRegistrationSaveDTO, personRegistrationResponseDTO);
            return this.ID != -1;
        }

        private bool _update()
        {
            return ClubProvider.Update(_clubSaveUpdateDTO, clubUpdateResponseDTO);
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
                    return _update();

            }

            return false;
        }

        public static List<ClubGetAllResponseDTO> GetAll(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");
            }
            if (pageSize > 100)
            {
                throw new ArgumentException("Page size cannot exceed 100 items per request.", nameof(pageSize));
            }
            return ClubProvider.GetAll(pageNumber, pageSize);
        }


        public static ClubFindByIDResponseDTO Find(int id)
        {
            ClubFindByIDResponseDTO clubFindResponseDTO = ClubProvider.GetByID(id);

            return clubFindResponseDTO;
        }

        public static bool Delete(int id)
        {
            return ClubProvider.Delete(id);
        }

    }
}
