using System;

namespace Grand.Plugin.Payments.AllBank.Iyzico
{
    class StringHelper
    {
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
