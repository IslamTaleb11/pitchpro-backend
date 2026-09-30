using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.User;
using DataAccessLayer.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class User
    {
        public int ID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleID { get; set; }
        public int PersonID { get; set; }
        public DateTime EmailVerifiedAt { get; set; }
        public string EmailVerificationTokenHash { get; set; }
        public DateTime EmailVerificationTokenExpiresAt { get; set; }
        public enum enMode
        {
            addNew = 0,
            update = 1
        }

        public enMode mode { get; set; }

        private  UserRegistrationSaveDTO _userRegistrationSaveDTO { get; set; }

        public UserRegistrationResponseDTO userRegistrationResponseDTO = new UserRegistrationResponseDTO();

        private IEmailService _emailService =>
            AppServicesHelper.ServiceProvider.GetRequiredService<IEmailService>();

        private IConfiguration _configuration =>
            AppServicesHelper.ServiceProvider.GetRequiredService<IConfiguration>();


        private void _InitializeFromRegistrationRequestDTO(UserRegistrationRequestDTO dto)
        {
            this.Email = dto.Email;
            this.Password = dto.Password;
            this.RoleID = dto.RoleID;
            this.PersonID = dto.PersonID;
        }

        private void _InitializeUserSaveDTO()
        {
            _userRegistrationSaveDTO = new UserRegistrationSaveDTO();
            _userRegistrationSaveDTO.Email = this.Email;
            _userRegistrationSaveDTO.Password = this.Password;
            _userRegistrationSaveDTO.RoleID = this.RoleID;
            _userRegistrationSaveDTO.PersonID = this.PersonID;
        }

        public User(UserRegistrationRequestDTO userRegistrationRequestDTO)
        {
            _InitializeFromRegistrationRequestDTO(userRegistrationRequestDTO);
            _InitializeUserSaveDTO();
            mode = enMode.addNew;

        }

        private User(UserFindResponseDTO userFindResponseDTO)
        {
            this.Email = userFindResponseDTO.Email;
            this.Password = userFindResponseDTO.Password;
            this.RoleID = userFindResponseDTO.RoleID;
            this.PersonID = userFindResponseDTO.PersonID;
        }

        private void _hashPassword()
        {
            _userRegistrationSaveDTO.Password = PasswordHasher.HashPassword(_userRegistrationSaveDTO.Password);
        }

        private async Task<bool> _addNew()
        {
            if (UserProvider.IsEmailExists(_userRegistrationSaveDTO.Email))
            { 
                throw new DuplicateEmailException(_userRegistrationSaveDTO.Email);
            }
            if (!PersonProvider.DoesPersonExist(_userRegistrationSaveDTO.PersonID))
            {
                throw new PersonNotFoundException(_userRegistrationSaveDTO.PersonID);
            }
            if (!RoleProvider.DoesRoleExist(_userRegistrationSaveDTO.RoleID))
            {
                throw new RoleNotFoundException(_userRegistrationSaveDTO.RoleID);
            }
            if (PersonProvider.DoesPersonHaveUser(_userRegistrationSaveDTO.PersonID))
            {
                throw new PersonAlreadyLinkedException(_userRegistrationSaveDTO.PersonID);
            }

            _hashPassword();

            string RoleName = await Role.GetRoleName(this.RoleID);

            string verificationUrl = "";

            if (RoleName == "President")
            {
                string token = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

                string tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));

                this.EmailVerificationTokenHash = tokenHash;
                this.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

                _userRegistrationSaveDTO.EmailVerificationTokenHash = tokenHash;
                _userRegistrationSaveDTO.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

                string frontend = _configuration["FrontendUrl"];

                verificationUrl =
                    $"{frontend}/verify-email?token={token}";

            }

            this.ID = UserProvider.AddNew(_userRegistrationSaveDTO, userRegistrationResponseDTO);

            if (this.ID != -1 && RoleName == "President")
            {
                await _emailService.SendEmailAsync(
    this.Email,
    "Verify Your Email - PitchPro",
    $@"
    <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:20px;border:1px solid #e5e5e5;border-radius:10px;'>

        <h2 style='color:#16a34a;text-align:center;'>Welcome to PitchPro! ⚽</h2>

        <p>Hi,</p>

        <p>Thank you for creating your PitchPro account.</p>

        <p>
            Before you can start managing your football club, please verify your email address by clicking the button below.
        </p>

        <div style='text-align:center;margin:35px 0;'>
            <a href='{verificationUrl}'
               style='background:#16a34a;
                      color:white;
                      padding:14px 28px;
                      text-decoration:none;
                      border-radius:6px;
                      font-weight:bold;
                      display:inline-block;'>
                Verify Email
            </a>
        </div>

        <p>
            If the button doesn't work, copy and paste the following link into your browser:
        </p>

        <p style='word-break:break-all;color:#2563eb;'>
            {verificationUrl}
        </p>

        <hr style='margin:30px 0;border:none;border-top:1px solid #e5e5e5;'>

        <p style='font-size:13px;color:#666;'>
            This verification link will expire in <strong>24 hours</strong>.
        </p>

        <p style='font-size:13px;color:#666;'>
            If you didn't create a PitchPro account, you can safely ignore this email.
        </p>

        <p style='margin-top:30px;'>
            Thanks,<br/>
            <strong>The PitchPro Team</strong>
        </p>

    </div>");
            }

            return this.ID != -1;
        }

        public async Task<bool> Save()
        {
            switch(mode)
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

        public static UserFindResponseDTO Find(int id)
        {
            UserFindResponseDTO userFindResponseDTO = UserProvider.GetByID(id);

            return userFindResponseDTO;
        }

        public static UserFindResponseDTO FindByEmail(string email)
        {
            UserFindResponseDTO userFindResponseDTO = UserProvider.GetByEmail(email);
            return userFindResponseDTO;
        }

        // Retrieve a user's club ID by user ID. Returns null if not found.
        public static async Task<int?> GetClubId(int userId)
        {
            // Calls the DAL method to fetch club ID.
            return await UserProvider.GetClubIdByUserId(userId);
        }

        public static async Task<User> FindAndGetFullObject(string email)
        {
            UserFindResponseDTO userFindResponseDTO = FindByEmail(email);

            User user = new User(userFindResponseDTO);

            return user;
        }

        // Verifies a user's email using the raw token from the email link.
        public static bool VerifyEmail(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));

            return UserProvider.VerifyEmail(tokenHash);
        }
    }
}
