using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Web;

namespace FooBox
{
    public static class Utilities
    {
        public const string LetterDigitChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const string IdChars = "abcdefghijklmnopqrstuvwxyz0123456789!@^_-";
        private static string[] _sizeUnits = new string[] { "B", "KB", "MB", "GB" };

        private static int _randomSeed = (new Random()).Next();
        [ThreadStatic]
        private static Random _random;

        public static Random Random
        {
            get
            {
                if (_random == null)
                {
                    int seed = System.Threading.Interlocked.Increment(ref _randomSeed);
                    _random = new Random(seed);
                }
                return _random;
            }
        }

        public static string GenerateRandomString(string alphabet, int length)
        {
            char[] c = new char[length];

            for (int i = 0; i < c.Length; i++)
                c[i] = alphabet[Random.Next(0, alphabet.Length)];

            return new string(c);
        }

        public static string GetParentFullName(string fullName)
        {
            if (fullName.Length != 0)
                return fullName.Remove(fullName.LastIndexOf('/'));

            return fullName;
        }

        public static string ComputeSha256Hash(string fileName)
        {
            byte[] buffer = new byte[4096 * 4];
            int bytesRead;

            // Simultaneously hash the file and write it out to a temporary file.

            using (var hashAlgorithm = System.Security.Cryptography.SHA256.Create())
            {
                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                return (new SoapHexBinary(hashAlgorithm.Hash)).ToString();
            }
        }

        public static void DeleteDirectoryRecursive(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectoryRecursive(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        public static string GenerateNewName(string originalDisplayName, bool isDocument, string key)
        {
            if (isDocument)
            {
                int indexOfLastDot = originalDisplayName.LastIndexOf('.');

                if (indexOfLastDot != -1 && indexOfLastDot != originalDisplayName.Length - 1)
                {
                    string firstPart = originalDisplayName.Substring(0, indexOfLastDot);
                    string secondPart = originalDisplayName.Substring(indexOfLastDot + 1, originalDisplayName.Length - (indexOfLastDot + 1));

                    return firstPart + " (" + key + ")." + secondPart;
                }
            }

            return originalDisplayName + " (" + key + ")";
        }

        public static string NormalizeFullName(string fullName)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var name in fullName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append('/');
                sb.Append(name);
            }

            return sb.ToString();
        }

        public static string SizeToString(long size)
        {
            if (size == 0)
                return "0";

            double s = size;
            int i = 0;

            while (s >= 1024 && i < _sizeUnits.Length - 1)
            {
                s /= 1024;
                i++;
            }

            return s.ToString("#,#.##") + " " + _sizeUnits[i];
        }

        public static bool ValidateFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var bad = new HashSet<char>("\0\\/:?*<>\"|");
            return !fileName.Any(bad.Contains);
        }

        public static bool ValidateString(string alphabet, string s)
        {
            var set = new HashSet<char>(alphabet);
            return s.All(set.Contains);
        }
    }
}