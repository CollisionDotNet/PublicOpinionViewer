using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public static class TextsSaveLoader
    {
        private const string uploadedTextsDirName = @"Texts\Uploaded Texts";
        private const string parsedPostsDirName = @"Texts\Parsed Posts";
        private const string mlExamplesFilePath = @"Texts\ML Examples\Examples.txt";
        /// <summary>
        /// Сохраняет объекты SentimentText в файлы. Если текст является постом, то первая строка в файле - текст поста, а остальные - тексты комментариев к посту
        /// </summary>
        /// <param name="posts">Перечисление объектов Post для сохранения</param>
        /// <param name="saveDirectory">Путь к директории для сохранения файлов. Название включает метку даты и времени сохранения</param>
        public static void Save(IEnumerable<SentimentText> texts, out string saveDirectory)
        {
            int fileNum = 1;
            string curTimeLabel = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss");   
            if(texts is IEnumerable<Post>)
                saveDirectory = @$"{parsedPostsDirName}\{curTimeLabel}";
            else
                saveDirectory = @$"{uploadedTextsDirName}\{curTimeLabel}";
            Directory.CreateDirectory(saveDirectory);
            foreach (SentimentText text in texts)
            {
                string[] flattenedText = Flatten(text);               
                File.WriteAllLines(@$"{saveDirectory}\{fileNum}", flattenedText);
                fileNum++;
            }
        }
        /// <summary>
        /// Представляет объект Post в виде массив строк
        /// <para>Две первые строки - ID автора поста и ID поста</para> 
        /// <para>Третья строка - текст поста</para> 
        /// <para>Остальные строки - тексты комментариев к посту (если есть)</para> 
        /// <para>Тексты поста и комментариев приводятся к виду без переноса строки</para> 
        /// </summary>
        /// <param name="post">Объект типа Post для представления в виде набора строк</param>
        /// <returns>Массив строк, к которому был приведен объект Post</returns>
        public static string[] Flatten(SentimentText text)
        {
            List<string> lines;
            if (text is Post post)
            {
                lines =
                [
                    post.Id,
                    post.Author.Id,
                    (post.Author.Sex == null ? "" : post.Author.Sex.ToString()!),
                    (post.Author.BirthDate == null ? "" : post.Author.BirthDate.Value.ToString("d.M.yyyy")),
                    StringExtensions.AsSingleLineWithSpaces(post.Text)
                ];
                if (post.Comments != null)
                {
                    foreach (Post.Comment comment in post.Comments)
                    {
                        lines.AddRange(
                        [
                            comment.Author.Id,
                            (comment.Author.Sex == null ? "" : comment.Author.Sex.ToString()!),
                            (comment.Author.BirthDate == null ? "" : comment.Author.BirthDate.Value.ToString("d.M.yyyy")),
                            StringExtensions.AsSingleLineWithSpaces(comment.Text)
                        ]);
                    }
                }
            }
            else
            {
                lines = [text.Text];
            }       
            return lines.ToArray();
        }
        /// <summary>
        /// Конвертирует массив строк в объект типа Post
        /// <para>Две первые строки - ID автора поста и ID поста</para> 
        /// <para>Третья строка - текст поста</para> 
        /// <para>Остальные строки - тексты комментариев к посту (если есть)</para> 
        /// <param name="lines">Массив строк для конвертации в объект Post</param>
        /// <returns>Объект Post - результат конвертации</returns>
        /// <exception cref="ArgumentException"></exception>
        public static SentimentText DeflattenText(string[] lines)
        {
            SentimentText text;
            if (lines.Length != 1)
            {
                throw new ArgumentException("This string array can't be represented as text!");
            }
            else
            {
                text = new SentimentText(lines[0]);
            }
            return text;
        }
        /// <summary>
        /// Конвертирует массив строк в объект типа Post
        /// <para>Две первые строки - ID автора поста и ID поста</para> 
        /// <para>Третья строка - текст поста</para> 
        /// <para>Остальные строки - тексты комментариев к посту (если есть)</para> 
        /// <param name="lines">Массив строк для конвертации в объект Post</param>
        /// <returns>Объект Post - результат конвертации</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Post DeflattenPost(string[] lines)
        {
            Post post;
            if(lines.Length < 5)
            {
                throw new ArgumentException("This string array can't be represented as post!");
            }
            else if(lines.Length == 5)
            {
                post = new Post(
                    lines[0], 
                    lines[4], 
                    new Author(lines[1]) 
                    { 
                        Sex = lines[2] == "" ? null : (Sex)Enum.Parse(typeof(Sex), lines[2]), 
                        BirthDate = lines[3] == "" ? null : DateOnly.ParseExact(lines[3], "d.M.yyyy") 
                    }, 
                    null, 
                    MediaSource.VK
                );
            }
            else
            {
                post = new Post(
                    lines[0], 
                    lines[4], 
                    new Author(lines[1]) 
                    { 
                        Sex = lines[2] == "" ? null : (Sex)Enum.Parse(typeof(Sex), lines[2]), 
                        BirthDate = lines[3] == "" ? null : DateOnly.ParseExact(lines[3], "d.M.yyyy")
                    }, 
                    [], 
                    MediaSource.VK
                );      
                List<Post.Comment> comments = new List<Post.Comment>();
                for(int i = 5; i < lines.Length; i += 4) 
                {
                    comments.Add(new Post.Comment(lines[i + 3], new Author(lines[i])
                    {
                        Sex = lines[i + 1] == "" ? null : (Sex)Enum.Parse(typeof(Sex), lines[i + 1]),
                        BirthDate = lines[i + 2] == "" ? null : DateOnly.ParseExact(lines[i + 2], "d.M.yyyy")
                    }));
                }
                post.Comments = comments.ToArray();
            }
            return post;
        }
        /// <summary>
        /// Читает файлы из заданной директории и представляет каждый файл в виде объекта типа SentimentText        
        /// </summary>
        /// <param name="directory">Директория, где хранятся файлы сохраненных постов</param>
        /// <returns>Перечисление объектов SentimentText, тектовые представления которых сохранены в заданной директории</returns>
        public static IEnumerable<SentimentText> LoadTexts(string directory)
        {
            string[] textFilePaths = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();
            IEnumerable<SentimentText> texts = textFilePaths.Select(fp => DeflattenText(File.ReadAllLines(fp)));
            return texts;
        }
        /// <summary>
        /// Читает файлы из заданной директории и представляет каждый файл в виде объекта типа Post
        /// <para>Содержимое каждого из файлов - массив строк</para>
        /// <para>Две первые строки - ID автора поста и ID поста</para> 
        /// <para>Третья строка - текст поста</para> 
        /// <para>Остальные строки - тексты комментариев к посту (если есть)</para> 
        /// </summary>
        /// <param name="directory">Директория, где хранятся файлы сохраненных постов</param>
        /// <returns>Перечисление объектов Post, тектовые представления которых сохранены в заданной директории</returns>
        public static IEnumerable<Post> LoadPosts(string directory)
        {
            string[] postFilePaths = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();
            IEnumerable<Post> posts = postFilePaths.Select(fp => DeflattenPost(File.ReadAllLines(fp)));
            return posts;
        }      
        public static void SaveNewMLExample(string text, Sentiment.Label label)
        {
            string newline = $"{text}|{(int)label}\n";
            File.AppendAllText(mlExamplesFilePath, newline);
        }
    }
}
