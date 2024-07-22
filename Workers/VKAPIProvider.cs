using System.Diagnostics;
using System.Text;
using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    //Обёртка для упрощенного вызова методов VKApi через HTTP-запросы и получения JSON ответов
    public class VKAPIProvider
    {
        public const int maxPostsPerRequest = 100;
        public const int maxNewsfeedPostsPerRequest = 200;
        public const int maxCommentsPerRequest = 100;
        private const string accessToken = "a10668e4a10668e4a10668e4a6a21e5b6faa106a10668e4c73058500f57311d7edf640b";
        private const string apiVersion = "5.199";
        private const int requestTimeout = 200; // Задержка между запросами в миллисекундах во избежание ошибки too many requests
        private readonly HttpClient httpClient;
        private Stopwatch? stopwatch;
        public VKAPIProvider()
        {
            httpClient = new HttpClient();
        }
        /// <summary>
        /// Реализует метод wall.get() VKApi для получения информации о постах  
        /// </summary>
        /// <param name="ownerId">ID пользователя или сообщества, со стены которого нужно получить посты</param>
        /// <param name="startIndex">Стартовый индекс поста, начиная с которого нужно сделать выборку</param>
        /// <param name="count">Количество постов (до 100)</param>
        /// <returns>JSON список с информацией о постах</returns>
        public async Task<string> ExecuteWallGetAsync(string ownerId, int startIndex, int count)
        {
            if (count < 1 || count > maxPostsPerRequest)
                throw new ArgumentOutOfRangeException("count");
            StringBuilder url = new StringBuilder();
            url.Append("https://api.vk.com/method/wall.get?");
            url.Append($"access_token={accessToken}");
            url.Append($"&owner_id={ownerId}");
            url.Append($"&offset={startIndex}");
            url.Append($"&count={count}");
            url.Append($"&v={apiVersion}");
            stopwatch = Stopwatch.StartNew();
            string responseJSON = await httpClient.GetStringAsync(url.ToString());
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds < requestTimeout)
                Thread.Sleep(requestTimeout - (int)stopwatch.ElapsedMilliseconds);
            return responseJSON;
        }
        /// <summary>
        /// Реализует метод wall.getComments() VKApi для получения информации о комментариях к посту
        /// </summary>
        /// <param name="ownerId">ID пользователя или сообщества, на стене которого расположен пост с комментариями</param>
        /// <param name="postId">ID поста в пределах стены пользователя или сообщества</param>
        /// <param name="startIndex">Стартовый индекс комментария, начиная с которого нужно сделать выборку</param>
        /// <param name="count">Количество комментариев (до 100)</param>
        /// <returns>JSON список с информацией о комментариях</returns>
        public async Task<string> ExecuteWallGetCommentsAsync(string ownerId, string postId, int startIndex, int count)
        {
            if (count < 1 || count > maxCommentsPerRequest)
                throw new ArgumentOutOfRangeException("count");
            StringBuilder url = new StringBuilder();
            url.Append("https://api.vk.com/method/wall.getComments?");
            url.Append($"access_token={accessToken}");
            url.Append($"&owner_id={ownerId}");
            url.Append($"&post_id={postId}");
            url.Append($"&offset={startIndex}");
            url.Append($"&count={count}");
            url.Append($"&v={apiVersion}");
            stopwatch = Stopwatch.StartNew();
            string responseJSON = await httpClient.GetStringAsync(url.ToString());
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds < requestTimeout)
                Thread.Sleep(requestTimeout - (int)stopwatch.ElapsedMilliseconds);
            return responseJSON;
        }
        /// <summary>
        /// Реализует метод newsfeed.search() VKApi для получения списка постов на заданную тему
        /// </summary>
        /// <param name="query">Запрос для поиска постов</param>
        /// <param name="count">Количество постов (до 200)</param>
        /// <param name="startFromLabel">Метка, начиная с которой необходимо формировать выборку</param>
        /// <returns>JSON список с информацией о постах</returns>
        public async Task<string> ExecuteNewsfeedSearchAsync(string query, int count, string? startFromLabel = null)
        {
            if (count < 1 || count > maxNewsfeedPostsPerRequest)
                throw new ArgumentOutOfRangeException("count");
            StringBuilder url = new StringBuilder();
            url.Append("https://api.vk.com/method/newsfeed.search?");
            url.Append($"access_token={accessToken}");
            url.Append($"&q={query}");
            url.Append($"&count={count}");
            if (startFromLabel != null)
                url.Append($"&start_from={startFromLabel}");
            url.Append($"&v={apiVersion}");
            stopwatch = Stopwatch.StartNew();
            string responseJSON = await httpClient.GetStringAsync(url.ToString());
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds < requestTimeout)
                Thread.Sleep(requestTimeout - (int)stopwatch.ElapsedMilliseconds);
            return responseJSON;
        }
        /// <summary>
        /// Реализует метод newsfeed.search() VKApi для получения списка постов на заданную тему за заданный промежуток времени
        /// </summary>
        /// <param name="query">Запрос для поиска постов</param>
        /// <param name="count">Количество постов (до 200)</param>
        /// <param name="startTime">Начало промежутка времени, за который производится выборка постов</param>
        /// <param name="endTime">Конец промежутка времени, за который производится выборка постов</param>
        /// <param name="startFromLabel">Метка, начиная с которой необходимо формировать выборку</param>
        /// <returns>JSON список с информацией о постах</returns>
        public async Task<string> GetPostsByQueryAndTimeAsync(string query, int count, DateTimeOffset startTime, DateTimeOffset endTime, string? startFromLabel = null)
        {
            if (count < 1 || count > maxPostsPerRequest)
                throw new ArgumentOutOfRangeException("count");
            long startTimeUNIX = startTime.ToUnixTimeSeconds();
            long endTimeUNIX = endTime.ToUnixTimeSeconds();
            StringBuilder url = new StringBuilder();
            url.Append("https://api.vk.com/method/newsfeed.search?");
            url.Append($"access_token={accessToken}");
            url.Append($"&q={query}");
            url.Append($"&count={count}");
            if (startFromLabel != null)
                url.Append($"&start_from={startFromLabel}");
            url.Append($"&start_time={startTimeUNIX}");
            url.Append($"&end_time={endTimeUNIX}");
            string responseJSON = await httpClient.GetStringAsync(url.ToString());
            Thread.Sleep(requestTimeout);
            return responseJSON;
        }
        public async Task<string> ExecuteUsersGetAsync(string userIds, string fields)
        {
            StringBuilder url = new StringBuilder();
            url.Append("https://api.vk.com/method/users.get?");
            url.Append($"access_token={accessToken}");
            url.Append($"&user_ids={userIds}");
            url.Append($"&fields={fields}");
            url.Append($"&v={apiVersion}");
            stopwatch = Stopwatch.StartNew();
            string responseJSON = await httpClient.GetStringAsync(url.ToString());
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds < requestTimeout)
                Thread.Sleep(requestTimeout - (int)stopwatch.ElapsedMilliseconds);
            return responseJSON;
        }
    }
}
