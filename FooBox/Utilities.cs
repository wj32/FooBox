using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FooBox
{
    public static class Utilities
    {
        public static string GenerateRandomString(string alphabet, int length)
        {
            Random r = new Random();
            char[] c = new char[length];

            for (int i = 0; i < c.Length; i++)
                c[i] = alphabet[r.Next(0, alphabet.Length)];

            return new string(c);
        }
    }
}