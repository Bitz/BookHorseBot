using System;
namespace BookHorseBot.Functions
{
    class Utils
    {
        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (String.IsNullOrEmpty(s))
            {
                return String.Empty;
            }
            // Return char and concat substring.
            return Char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string FormatNumber(long num)
        {
            long i = (long)Math.Pow(10, (int)Math.Max(0, Math.Log10(num) - 2));
            num = num / i * i;

            if (num >= 1000000000)
                return (num / 1000000000D).ToString("0.##") + "B";
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##") + "M";
            if (num >= 1000)
                return (num / 1000D).ToString("0.##") + "K";

            return num.ToString("#,0");
        }
    }
}
