namespace GenZCoders.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash)) return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Legacy rows may hold a value that isn't a BCrypt hash: treat it as a wrong password, not a 500.
                return false;
            }
        }
    }
}
