using BCrypt.Net;
namespace BusinessLayer.Helpers
{
    public static class PasswordHasher
    {
        // Hash the password before saving to the Database
        public static string HashPassword(string password)
        {
            // "WorkFactor" 12 is a good balance between security and speed
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        // Compare the plain text login password with the hashed password from the DB
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
