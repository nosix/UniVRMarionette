namespace VRMarionette
{
    public static class StringExtensions
    {
        public static string RemoveSpace(this string value)
        {
            return value.Replace(" ", "");
        }
    }
}