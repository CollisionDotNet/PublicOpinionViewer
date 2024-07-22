using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public static class StringExtensions
    {
        public static string RemoveConsecutiveChars(this string str, char toRemove)
        {
            return string.Join(toRemove, str.Split(toRemove, StringSplitOptions.RemoveEmptyEntries));
        }
        public static string RemoveConsecutiveChars(this string str, string toRemove)
        {
            return string.Join(toRemove, str.Split(toRemove, StringSplitOptions.RemoveEmptyEntries));
        }
        // Заменяем все переносы строк на пробелы, дублирующиеся пробелы удаляем
        public static string AsSingleLineWithSpaces(string toFormat)
        {
            return toFormat.Replace("\n", " ").Replace("\r", " ").RemoveConsecutiveChars(' ');
       }
        public static string ShortenText(string str, int maxLength, string trailingStr = "...")
        {
            if (maxLength > str.Length)
                return str;
            str = str.Substring(0, maxLength);
            str = str.Substring(0, str.LastIndexOf(' ') + 1) + trailingStr;
            return str;
        }
        public static string ToString(this string text, Sex sex)
        {
            switch (sex)
            {
                case Sex.Female:
                    return "Женский";
                case Sex.Male:
                    return "Мужской";
            }
            return sex.ToString();
        }
    }
}
