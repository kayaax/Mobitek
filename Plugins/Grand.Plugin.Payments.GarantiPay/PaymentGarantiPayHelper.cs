using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Plugin.Payments.GarantiPay
{
    public static class PaymentGarantiPayHelper
    {
        public static string GetSHA1(string SHA1Data)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            string HashedPassword = SHA1Data;
            Encoding encoding = new CustomEncoding();
            byte[] hashbytes = encoding.GetBytes(HashedPassword);
            byte[] inputbytes = sha.ComputeHash(hashbytes);
            return GetHexaDecimal(inputbytes);
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
        public static string CodeCreate(string text)
        {
            try
            {
                string strReturn = text.Trim();

                strReturn = strReturn.Replace("ğ", "g");
                strReturn = strReturn.Replace("Ğ", "G");
                strReturn = strReturn.Replace("ü", "u");
                strReturn = strReturn.Replace("Ü", "U");
                strReturn = strReturn.Replace("ş", "s");
                strReturn = strReturn.Replace("Ş", "S");
                strReturn = strReturn.Replace("ı", "i");
                strReturn = strReturn.Replace("İ", "I");
                strReturn = strReturn.Replace("ö", "o");
                strReturn = strReturn.Replace("Ö", "O");
                strReturn = strReturn.Replace("ç", "c");
                strReturn = strReturn.Replace("Ç", "C");
                strReturn = strReturn.Replace("-", "+");
                strReturn = strReturn.Replace(" ", "+");
                strReturn = strReturn.Trim();
                strReturn = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9+]").Replace(strReturn, "");
                strReturn = strReturn.Trim();
                strReturn = strReturn.Replace("+", "_");
                return strReturn;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static string EncryptVirtualPosCredentials(string terminalId, string terminalUserIdPassword)
        {

            if (string.IsNullOrEmpty(terminalId))
                throw new ArgumentNullException("terminalId");

            if (string.IsNullOrEmpty(terminalUserIdPassword))
                throw new ArgumentNullException("terminalUserIdPassword");

            //if the terminal id Lenght is less than 9 digit
            if (terminalId.Length != 9)
            {

                var leftoverLenght = 9 - terminalId.Length;
                for (int i = 0; i < leftoverLenght; i++)
                    terminalId = $"0{terminalId}";
            }

            string encryptedValue = String.Format("{0}{1}", terminalUserIdPassword, terminalId);

            return GetSHA1(encryptedValue).ToUpper();
        }
        public static string EncodeDecimalPaymentAmount(double amount)
        {
            ulong totalamount = 0;
            totalamount = (ulong)(Math.Round(amount, 2));

            return totalamount.ToString();
        }
        public static decimal ToDecimal(string paidPrice)
        {
            var number = Convert.ToDecimal(paidPrice,CultureInfo.InvariantCulture.NumberFormat);
            var paid = number / 100M;
            return Math.Round(paid,2);
        }
    }
}