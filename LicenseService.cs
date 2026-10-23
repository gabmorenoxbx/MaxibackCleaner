
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MaxibackCleaner.Services
{
    public static class LicenseService
    {
        // Código gratuito compartido.
        private const string LicenseCode = "MAXIBACK-FREE-2026";

        private static readonly string LicenseFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MaxibackCleaner");

        private static readonly string LicenseFile =
            Path.Combine(LicenseFolder, "license.dat");

        public static bool IsActivated()
        {
            try
            {
                if (!File.Exists(LicenseFile))
                    return false;

                string savedHash = File.ReadAllText(LicenseFile).Trim();
                string expectedHash = Hash(LicenseCode);

                return string.Equals(
                    savedHash,
                    expectedHash,
                    StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public static bool Activate(string enteredCode)
        {
            if (string.IsNullOrWhiteSpace(enteredCode))
                return false;

            if (!string.Equals(
                enteredCode.Trim(),
                LicenseCode,
                StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(LicenseFolder);
                File.WriteAllText(
                    LicenseFile,
                    Hash(LicenseCode));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Hash(string value)
        {
            byte[] bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

            return Convert.ToHexString(bytes);
        }
    }
}
