using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.User;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.Providers;
using System.Transactions;
using BusinessLayer.Exceptions;
using Microsoft.AspNetCore.Http;
using BusinessLayer.Services.Azure;
using BusinessLayer.Services;

namespace BusinessLayer
{
    public class Club
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public IFormFile Crest { get; set; }
        public string PrimaryIdentityColor { get; set; }
        public string ContactNumber { get; set; }

        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode { get; set; }

        private ClubSaveDTO _clubSaveDTO { get; set; }
        private ClubSaveUpdateDTO _clubSaveUpdateDTO { get; set; }
        private ClubRegistrationRequest _clubRegistrationRequest = new ClubRegistrationRequest();
        public ClubRegistrationResponseDTO clubRegistrationResponseDTO = new ClubRegistrationResponseDTO();
        public ClubUpdateResponseDTO clubUpdateResponseDTO = new ClubUpdateResponseDTO();
        private UserRegistrationRequestDTO _userRegistrationRequestDTO;
        private PersonRegistrationRequestDTO _personRegistrationRequestDTO;
        private Person _person;
        private User _user;
        private string _uploadedCrestUrl;

        private void _InitializeFromRegistrationRequestDTO()
        {
            this.Name = _clubRegistrationRequest.Name;
            this.Crest = _clubRegistrationRequest.Crest;
            this.PrimaryIdentityColor = _clubRegistrationRequest.PrimaryIdentityColor;
            this.ContactNumber = _clubRegistrationRequest.ContactNumber;
        }

        private void _InitializeClubSaveDTO()
        {
            _clubSaveDTO = new ClubSaveDTO();
            _clubSaveDTO.Name = this.Name;
            _clubSaveDTO.Crest = _uploadedCrestUrl;
            _clubSaveDTO.PrimaryIdentityColor = this.PrimaryIdentityColor;
            _clubSaveDTO.ContactNumber = this.ContactNumber;
        }

        public Club(ClubRegistrationRequest clubRegistrationRequestDTO)
        {
            _clubRegistrationRequest = clubRegistrationRequestDTO;
            _InitializeFromRegistrationRequestDTO();
            _InitializeClubSaveDTO();
            mode = enMode.addNew;
        }

        private void _InitializeClubSaveUpdateDTO()
        {
            _clubSaveUpdateDTO = new ClubSaveUpdateDTO();
            _clubSaveUpdateDTO.ID = this.ID;
            _clubSaveUpdateDTO.Name = this.Name;
            _clubSaveUpdateDTO.Crest = _uploadedCrestUrl;
            _clubSaveUpdateDTO.PrimaryIdentityColor = this.PrimaryIdentityColor;
            _clubSaveUpdateDTO.ContactNumber = this.ContactNumber;
        }

        private void _InitializeFromUpdateRequestDTO(ClubUpdateRequestDTO dto)
        {
            this.ID = dto.ID;
            this.Name = dto.Name;
            this.Crest = dto.Crest;
            this.PrimaryIdentityColor = dto.PrimaryIdentityColor;
            this.ContactNumber = dto.ContactNumber;
        }

        public Club(ClubUpdateRequestDTO clubUpdateRequestDTO)
        {
            _InitializeFromUpdateRequestDTO(clubUpdateRequestDTO);
            mode = enMode.update;
        }

        private void _InitializePersonRequestDTO()
        {
            _personRegistrationRequestDTO = new PersonRegistrationRequestDTO();
            _personRegistrationRequestDTO.FirstName = _clubRegistrationRequest.FirstName;
            _personRegistrationRequestDTO.SecondName = _clubRegistrationRequest.SecondName;
            _personRegistrationRequestDTO.LastName = _clubRegistrationRequest.LastName;
            _personRegistrationRequestDTO.Gender = _clubRegistrationRequest.Gender;
            _personRegistrationRequestDTO.BirthDate = _clubRegistrationRequest.BirthDate;
            _personRegistrationRequestDTO.ClubID = this.ID;
            _person = new Person(_personRegistrationRequestDTO);
        }

        private void _InitializeUserRequestDTO()
        {
            _userRegistrationRequestDTO = new UserRegistrationRequestDTO();
            _userRegistrationRequestDTO.Email = _clubRegistrationRequest.Email;
            _userRegistrationRequestDTO.Password = _clubRegistrationRequest.Password;
            _userRegistrationRequestDTO.RoleID = RoleProvider.GetRoleIDByName("President");
            _userRegistrationRequestDTO.PersonID = _person.ID;

            _user = new User(_userRegistrationRequestDTO);
        }

        private async Task<bool> _addNew(BlobService blobService)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    if (this.Crest != null)
                    {
                        // We "await" the result. NOTE: You'll need to make _addNew 'async Task<bool>'
                        _uploadedCrestUrl = await blobService.UploadAndResizeImageAsync(this.Crest, AzureContainers.Club, 800, 800);
                    }

                    _InitializeClubSaveDTO();

                    this.ID = await ClubProvider.AddNew(_clubSaveDTO, clubRegistrationResponseDTO);

                    _InitializePersonRequestDTO();

                    if (!_person.Save()) return false;

                    _InitializeUserRequestDTO();
                    if (await _user.Save() != true) return false;

                    scope.Complete();
                    return true;
                }
                catch (BaseException ex)
                {
                    throw new BaseException(ex.Message);
                }
            }
        }

        private bool _update()
        {
            return ClubProvider.Update(_clubSaveUpdateDTO, clubUpdateResponseDTO);
        }

        public async Task<bool> Save(BlobService blobService)
        {
            switch(mode)
            {
                case enMode.addNew:
                    try
                    {
                        return await _addNew(blobService);

                    }
                    catch (Exception)
                    {
                        throw;
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
