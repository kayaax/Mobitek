using Grand.Plugin.Payments.AllBank.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Plugin.Payments.AllBank
{
    public static class AllBankHelper
    {
        public static string GetDisplayName(this Enum value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            DisplayAttribute attribute = value.GetType().GetField(value.ToString())
                .GetCustomAttributes(typeof(DisplayAttribute), false).Cast<DisplayAttribute>().FirstOrDefault();

            if (attribute == null)
            {
                return value.ToString();
            }

            string propValue = attribute.Name;
            return propValue.ToString();
        }

        public static string EncodeExpireMonth(int month)
        {
            var expireMonth = (month.ToString().Length == 1) ? $"0{month.ToString()}" : month.ToString();
            return $"{expireMonth}";
        }
        public static string EncodeExpireYear(int year)
        {
            if (year.ToString().Length == 2)
            {
                var expireYear = $"20{year}";
                return expireYear;
            }
            return $"{year}";
        }
        public static decimal ToDecimal(string paidPrice)
        {
            decimal number;
            number = Convert.ToDecimal(paidPrice, CultureInfo.InvariantCulture);
            return Math.Round(number, 4);
        }
        public static string ToString(decimal value, bool invariantCulture = true)
        {
            if (invariantCulture) return value.ToString(CultureInfo.InvariantCulture);
            return value.ToString(CultureInfo.InvariantCulture);
        }
        public static string GetHexaDecimal(byte[] bytes)
        {
            StringBuilder s = new StringBuilder();
            int length = bytes.Length;
            for (int n = 0; n <= length - 1; n++)
            {
                s.Append($"{bytes[n],2:x}".Replace(" ", "0"));
            }
            return s.ToString();
        }
        public static string GetSha1(string sha1Data)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            string hashedPassword = sha1Data;
            Encoding encoding = new CustomEncoding();
            byte[] hashbytes = encoding.GetBytes(hashedPassword);
            byte[] inputbytes = sha.ComputeHash(hashbytes);
            return GetHexaDecimal(inputbytes);
        }

    }
}
