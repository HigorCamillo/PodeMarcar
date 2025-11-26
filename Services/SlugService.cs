using System.Text.RegularExpressions;

namespace MarcaAi.Backend.Services
{
    public static class SlugService
    {
        public static string GenerateSlug(string phrase)
        {
            string str = phrase.ToLower();
            // remove all accents
            str = Regex.Replace(str, "[áàäâã]", "a");
            str = Regex.Replace(str, "[éèëê]", "e");
            str = Regex.Replace(str, "[íìïî]", "i");
            str = Regex.Replace(str, "[óòöôõ]", "o");
            str = Regex.Replace(str, "[úùüû]", "u");
            str = Regex.Replace(str, "[ç]", "c");
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            // replace spaces with hyphens
            str = Regex.Replace(str, @"\s", "-");

            return str;
        }
    }
}
