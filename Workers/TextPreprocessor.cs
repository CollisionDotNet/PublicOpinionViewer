using Iveonik.Stemmers;
using System.Text;

namespace PublicOpinionViewer.Workers
{
    public class TextPreprocessor
    {
        public enum Language
        {
            English,
            Russian
        }
        //Язык предобрабатываемого текста
        public Language TextLanguage { get; init; }
        //Ссылки на файлы стоп-слов
        private Dictionary<Language, string> stopwordsFilePaths = new Dictionary<Language, string>()
        {
            { Language.English, @"NLP\Stopwords\English.txt" },
            { Language.Russian, @"NLP\Stopwords\Russian.txt" },
        };
        private HashSet<string> stopwords;
        //Стеммер из библиотеки StemmersNet
        private IStemmer stemmer;
        //Лемматизатор из библиотеки MyStem.Net, являющейся .NET оберткой для CLI-программы MyStem от Yandex
        private Mystem.Net.Mystem? lemmatizer;
        public TextPreprocessor(Language textLanguage)
        {
            TextLanguage = textLanguage;

            string stopwordsFilePath = stopwordsFilePaths[textLanguage];
            string[] stopwordsFromFile = File.ReadAllLines(stopwordsFilePath);
            stopwords = new HashSet<string>(stopwordsFromFile);

            switch (textLanguage)
            {
                case Language.English:
                    stemmer = new EnglishStemmer();
                    break;
                case Language.Russian:
                    stemmer = new RussianStemmer();
                    lemmatizer = new Mystem.Net.Mystem();
                    break;
                default:
                    throw new ArgumentException($"The language {textLanguage} is not supported!");
            }            
        }
        /// <summary>
        /// Выполняет приведение к нижнему регистру, очистку от символов, не являющихся буквами и токенизацию текста
        /// </summary>
        /// <param name="text">Токенизируемый текст</param>
        /// <returns>Набор токенов исходного текста</returns>
        public IEnumerable<string> Tokenize(string text)
        {
            text = text.ToLower();
            text = text.Replace('ё', 'е');
            text = text.Replace('-', ' ');
            text = string.Join(' ', text.Split(' ').Where(t => !(t.StartsWith("http://") || t.StartsWith("https://"))));
            text = new string(text.ToCharArray()
                .Select(c => char.IsLetter(c) ? c : ' ')
                .ToArray());
            return text.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
        /// <summary>
        /// Выполняет удаление из набора токенов стандартных стоп-слов, определенных в соответствии с языком текста
        /// </summary>
        /// <param name="tokens">Набор токенов</param>
        /// <returns>Набор токенов без стоп-слов</returns>
        public IEnumerable<string> RemoveStopwords(IEnumerable<string> tokens)
        {
            return tokens.Where(w => !stopwords.Contains(w) && w.Length > 2);
        }
        /// <summary>
        /// Выполняет удаление из набора токенов заданных стоп-слов
        /// </summary>
        /// <param name="tokens">Набор токенов</param>
        /// <param name="stopwords">HashSet стоп-слов</param>
        /// <returns></returns>
        public IEnumerable<string> RemoveStopwords(IEnumerable<string> tokens, HashSet<string> stopwords)
        {
            return tokens.Where(w => !stopwords.Contains(w) && w.Length > 2);
        }
        /// <summary>
        /// Выполняет стемминг токенов в соответствии с языком текста
        /// </summary>
        /// <param name="tokens">Набор токенов</param>
        /// <returns>Набор стемов токенов</returns>
        public IEnumerable<string> FindStems(IEnumerable<string> tokens)
        {
            return tokens.Select(stemmer.Stem);
        }
        /// <summary>
        /// Выполняет лемматизацию токенов
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> FindLemmasAsync(IEnumerable<string> tokens)
        {
            if (lemmatizer == null)
                throw new Exception($"MyStem lemmatization is not supported for this preprocessor options!");
            Encoding cp866 = Encoding.GetEncoding(866);
            IEnumerable<string> lemmas = await lemmatizer.Mystem.Lemmatize(string.Join(" ", tokens));
            lemmas = lemmas.Where(l => !string.IsNullOrWhiteSpace(l));
            lemmas = lemmas.Select(l => Encoding.UTF8.GetString(cp866.GetBytes(l)));
            return lemmas;
        }
        /// <summary>
        /// Выполняет лемматизацию текста
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> FindLemmasAsync(string text)
        {
            if (lemmatizer == null)
                throw new Exception($"MyStem lemmatization is not supported for this preprocessor options!");
            Encoding cp866 = Encoding.GetEncoding(866);
            IEnumerable<string> lemmas = await lemmatizer.Mystem.Lemmatize(text);
            lemmas = lemmas.Where(l => !string.IsNullOrWhiteSpace(l));
            lemmas = lemmas.Select(l => Encoding.UTF8.GetString(cp866.GetBytes(l)));
            return lemmas;
        }
    }
}
