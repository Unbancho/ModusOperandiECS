
namespace ModusOperandi.ECS.Utils.Extensions
{
    public static class StringExtensions
    {
        public static string Capitalized(this string s)
        {
            var charArray = s.ToCharArray();
            charArray[0] = charArray[0].ToString().ToUpper().ToCharArray()[0];
            return new string(charArray);
        }
    }
}