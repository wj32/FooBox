using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace FooBox
{
    public static class Utilities
    {
        private static string[] _sizeUnits = new string[] { "B", "KB", "MB", "GB" };

        public static string GenerateRandomString(string alphabet, int length)
        {
            Random r = new Random();
            char[] c = new char[length];

            for (int i = 0; i < c.Length; i++)
                c[i] = alphabet[r.Next(0, alphabet.Length)];

            return new string(c);
        }

        public static string GetParentFullName(string fullName)
        {
            if (fullName.Length != 0)
                return fullName.Remove(fullName.LastIndexOf('/'));

            return fullName;
        }

        public static void LockTableExclusive(this System.Data.Entity.Database database, string tableName)
        {
            database.ExecuteSqlCommand("SELECT TOP 1 Id FROM " + tableName + " WITH (TABLOCKX, HOLDLOCK)");
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

            while (s >= 1024 && i < _sizeUnits.Length)
            {
                s /= 1024;
                i++;
            }

            return s.ToString("#,#.##") + " " + _sizeUnits[i];
        }

        public static bool ValidateString(string alphabet, string s)
        {
            HashSet<char> set = new HashSet<char>(alphabet);
            return s.All(set.Contains);
        }
    }
}