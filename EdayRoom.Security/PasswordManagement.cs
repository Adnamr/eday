using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace EdayRoom.Security
{
    public class PasswordManagement
    {
        private static byte[] GenerateSaltToken()
        {
            int minSaltSize = int.Parse(ConfigurationManager.AppSettings["minSaltSize"]);
            int maxSaltSize = int.Parse(ConfigurationManager.AppSettings["maxSaltSize"]);
            // Generate a random number for the size of the salt.
            var random = new Random();
            int saltSize = random.Next(minSaltSize, maxSaltSize);
            // Allocate a byte array, which will hold the salt.
            var saltBytes = new byte[saltSize];
            // Initialize a random number generator.
            var rng = new RNGCryptoServiceProvider();
            // Fill the salt with cryptographically strong byte values.
            rng.GetNonZeroBytes(saltBytes);
            return saltBytes;
        }

        public static string GenerateRandomPassword(int passwordSize)
        {
            // Allocate a byte array, which will hold the salt.
            var passwordBytes = new byte[passwordSize];
            // Initialize a random number generator.
            var rng = new RNGCryptoServiceProvider();
            // Fill the salt with cryptographically strong byte values.
            rng.GetNonZeroBytes(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public static string SaltPassword(string password, string salt)
        {
            byte[] start = GenerateSaltToken();
            string str1 = Convert.ToBase64String(start);
            byte[] bt1 = Convert.FromBase64String(str1);
            bool tru = start == bt1;
            HashAlgorithm hash = new SHA256Managed();
            // compute hash of the password
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(password);
            // create salted byte array
            byte[] saltBytes = Convert.FromBase64String(salt);
            var plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];
            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }
            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }
            // compute salted hash

            byte[] saltedHashBytes = hash.ComputeHash(plainTextWithSaltBytes);
            string saltedHashValue = Convert.ToBase64String(saltedHashBytes);
            return saltedHashValue;
        }

        public static string GeneratePasswordHash(string password, out string salt)
        {
            if (password == null) throw new ArgumentNullException("password");

            HashAlgorithm hash = new SHA256Managed();
            // compute hash of the password
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(password);
            // create salted byte array
            byte[] saltBytes = GenerateSaltToken();
            var plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];
            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }
            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }
            // compute salted hash
            salt = Convert.ToBase64String(saltBytes);
            byte[] saltedHashBytes = hash.ComputeHash(plainTextWithSaltBytes);
            string saltedHashValue = Convert.ToBase64String(saltedHashBytes);
            return saltedHashValue;
        }
    }
}