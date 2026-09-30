using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BusinessLayer.Helpers
{
    public static class GeneralHelper
    {
        public static bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            // 1. Check file size (2MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                Console.WriteLine($"Image rejected: Size too large ({file.Length} bytes)");
                return false;
            }

            // 2. MIME Type - Expanded to include legacy/browser-specific types
            var allowedMimeTypes = new[] {
        "image/jpeg", "image/png", "image/webp", "image/jpg",
        "image/x-png", "image/pjpeg"
    };

            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                Console.WriteLine($"Image rejected: MIME type {file.ContentType} not allowed");
                return false;
            }

            // 3. Extension - Only check if filename isn't just "blob"
            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!string.IsNullOrEmpty(extension) && extension != ".blob")
            {
                if (!allowedExtensions.Contains(extension))
                {
                    Console.WriteLine($"Image rejected: Extension {extension} not allowed");
                    return false;
                }
            }

            return true;
        }

        public static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string ComputeSha256Hash(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }

        public static SecurityToken GenerateJWT(IEnumerable<Claim> claims, string jwtKey)
        {

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));
            
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return token;
        }


    }
}
