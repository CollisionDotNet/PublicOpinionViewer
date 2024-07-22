using Microsoft.Extensions.Hosting;
using PublicOpinionViewer.Models;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;

namespace PublicOpinionViewer.Workers
{
    public class VKAggregator
    {
        //Ссылка на файл, хранящий идентификаторы популярных групп
        private const string popularGroupsIDsFile = @"API\VK\PopularGroups.txt";
        private readonly VKAPIProvider apiProvider;
        private readonly string personsInfoFields = "bdate, sex";
        public VKAggregator()
        {
            apiProvider = new VKAPIProvider();
        }
        /// <summary>
        /// Выполняет сбор постов со стены конкретного пользователя или сообщества
        /// </summary>
        /// <param name="ownerId">owner_id пользователя или сообщества, на стене которого размещены посты</param>
        /// <param name="count">Количество постов</param>
        /// <param name="startIndex">Номер поста, начиная с которого нужно формировать выборку</param>
        /// <param name="ignoreEmptyText">Пропускать ли посты без текста?</param>
        /// <param name="withComments">Выполнять ли также сбор комментариев к постам?</param>
        /// <returns>Массив постов со стены пользователя или сообщества</returns>
        public async Task<Post[]?> GetOwnerPostsAsync(string ownerId, int count, bool withAuthorsInfo, int commentsPerPostCount = -1, int startIndex = 0, bool ignoreEmptyText = true)
        {
            List<Post> posts = new List<Post>();
            int postsParsed = 0;
            bool enough = false;
            while (!enough)
            {
                string response = await apiProvider.ExecuteWallGetAsync(ownerId, startIndex, VKAPIProvider.maxPostsPerRequest);
                startIndex += VKAPIProvider.maxPostsPerRequest;
                IEnumerable<Post>? thisRequestPosts = ParseJSONAsPosts(response);
                if(thisRequestPosts == null)
                {
                    return null;
                }
                if(thisRequestPosts.Count() < VKAPIProvider.maxPostsPerRequest) // Дошли до конца стены
                {
                    enough = true;
                }
                if (ignoreEmptyText)
                    thisRequestPosts = thisRequestPosts.Where(p => !string.IsNullOrWhiteSpace(p.Text));
                postsParsed += thisRequestPosts.Count();
                if (postsParsed >= count)
                {
                    thisRequestPosts = thisRequestPosts.SkipLast(postsParsed - count);
                    enough = true;
                }
                thisRequestPosts = thisRequestPosts.ToArray();
                if (commentsPerPostCount != 0)
                {
                    foreach (Post post in thisRequestPosts)
                    {
                        if (post.Comments != null)
                        {
                            await GetCommentsOfPostAsync(post, withAuthorsInfo, commentsPerPostCount, ignoreEmptyText);
                        }
                    }
                }
                posts.AddRange(thisRequestPosts);
            }
            if (withAuthorsInfo)
                await GetPostsAuthorsInfoAsync(posts);
            return posts.ToArray();
        }
        /// <summary>
        /// Выполняет сбор постов популярных сообществ
        /// </summary>
        /// <param name="postsOnGroupCount">Количество постов, получаемых со стены одного сообщества</param>
        /// <param name="withComments">Нужно ли извлекать комментарии к постам?</param>
        /// <returns>Массив постов популярных групп</returns>
        public async Task<Post[]?> GetPostsFromPopularGroupsAsync(int postsOnGroupCount, bool withAuthorsInfo, int commentsPerPostCount = -1)
        {
            IEnumerable<string> ownerIDs = File.ReadAllLines(popularGroupsIDsFile);
            List<Post> posts = new List<Post>(); // TODO
            foreach (string ownerId in ownerIDs)
            {
                Post[]? thisGroupPosts = await GetOwnerPostsAsync(ownerId, postsOnGroupCount, withAuthorsInfo, commentsPerPostCount: commentsPerPostCount);
                if(thisGroupPosts != null)
                    posts.AddRange(thisGroupPosts);
            }
            if (posts.Count == 0)
                return null;
            return posts.ToArray();
        }
        /// <summary>
        /// Выполняет сбор постов по заданной теме
        /// </summary>
        /// <param name="theme">Тема постов</param>
        /// <param name="count">Количество постов (до 1000)</param>
        /// <param name="withComments">Нужно ли извлекать комментарии к постам?</param>
        /// <returns>Массив постов по теме</returns>
        public async Task<Post[]?> GetPostsByThemeAsync(string theme, int count, bool withAuthorsInfo, int commentsPerPostCount = -1, bool ignoreEmptyText = true)
        {
            if (count > 1000)
                throw new ArgumentOutOfRangeException("count");
            List<Post> posts = new List<Post>();
            int postsParsed = 0;
            bool enough = false;
            string? nextFromLabel = null;
            while (!enough)
            {
                string response = await apiProvider.ExecuteNewsfeedSearchAsync(theme, VKAPIProvider.maxNewsfeedPostsPerRequest, nextFromLabel);
                IEnumerable<Post>? thisRequestPosts = ParseJSONAsNewsfeedPosts(response, out nextFromLabel);
                if (thisRequestPosts == null)
                {
                    return null;
                }
                if (nextFromLabel == null) // Дошли до конца поиска либо больше нельзя выгружать посты по поиску (лимит 1000)
                {
                    enough = true;
                }
                if (ignoreEmptyText)
                    thisRequestPosts = thisRequestPosts.Where(p => !string.IsNullOrWhiteSpace(p.Text));
                postsParsed += thisRequestPosts.Count();
                if (postsParsed >= count)
                {
                    thisRequestPosts = thisRequestPosts.SkipLast(postsParsed - count);
                    enough = true;
                }
                thisRequestPosts = thisRequestPosts.ToArray();                
                if (commentsPerPostCount != 0)
                {
                    foreach (Post post in thisRequestPosts)
                    {                        
                        if (post.Comments != null)
                        {
                            await GetCommentsOfPostAsync(post, withAuthorsInfo, commentsPerPostCount, ignoreEmptyText);
                        }
                    }
                }               
                posts.AddRange(thisRequestPosts);
            }
            if (withAuthorsInfo)
                await GetPostsAuthorsInfoAsync(posts);
            return posts.ToArray();
        }
        // Парсит строку JSON в перечисление Post
        private IEnumerable<Post>? ParseJSONAsPosts(string json)
        {
            JsonNode responseJSON = JsonNode.Parse(json)!;
            if (responseJSON["error"] != null)
            {
                Console.WriteLine(responseJSON["error"]);
                return null;
            }
            JsonArray postsToParse = (JsonArray)responseJSON["response"]!["items"]!;
            IEnumerable<Post> posts = postsToParse.Select(post => new Post(
                source: MediaSource.VK,
                id: post!["id"]!.GetValue<int>().ToString(),
                author: new Author(
                    id: post!["owner_id"]!.GetValue<int>().ToString()
                ),
                text: post["text"]!.GetValue<string>(),                           
                comments: post["comments"]!["count"]!.GetValue<int>() == 0 ? null : []
            ));           
            return posts;
        }
        // Парсит строку JSON в перечисление Post при условии, что JSON является результатом newsfeed.search()
        private IEnumerable<Post>? ParseJSONAsNewsfeedPosts(string json, out string? nextFromMark)
        {
            JsonNode responseJSON = JsonNode.Parse(json)!;
            if (responseJSON["error"] != null)
            {
                Console.WriteLine(responseJSON["error"]);
                nextFromMark = null;
                return null;
            }
            JsonNode? nextFromNode = responseJSON["response"]!["next_from"];
            if (nextFromNode == null)
                nextFromMark = null;
            else
                nextFromMark = nextFromNode.GetValue<string>();
            JsonArray postsToParse = (JsonArray)responseJSON["response"]!["items"]!;
            IEnumerable<Post> posts = postsToParse.Select(post => new Post(
                source: MediaSource.VK,
                id: post!["id"]!.GetValue<int>().ToString(),
                author: new Author(
                    id: post!["owner_id"]!.GetValue<int>().ToString()
                ),              
                text: post["text"]!.GetValue<string>(),
                comments: post["comments"]!["count"]!.GetValue<int>() == 0 ? null : []
            ));            
            return posts;
        }
        // Парсит строку JSON в перечисление Post.Comment
        private IEnumerable<Post.Comment>? ParseJSONAsComments(string json)
        {
            JsonNode responseJSON = JsonNode.Parse(json)!;
            if (responseJSON["error"] != null)
            {
                Console.WriteLine(responseJSON["error"]);
                return null;
            }
            JsonArray commentsToParse = (JsonArray)responseJSON["response"]!["items"]!;
            IEnumerable<Post.Comment> comments = commentsToParse.Select(comment => new Post.Comment(
                text: comment!["text"]!.GetValue<string>(),
                author: new Author(
                    id: comment!["from_id"]!.GetValue<int>().ToString()
                )
            ));
            return comments;
        }
        private async Task<Dictionary<string, (int? sex, string? bdate)>?> GetAuthorsDataAsync(string authorIDs)
        {
            string json = await apiProvider.ExecuteUsersGetAsync(authorIDs, personsInfoFields);
            JsonNode responseJSON = JsonNode.Parse(json)!;
            if (responseJSON["error"] != null)
            {
                Console.WriteLine(responseJSON["error"]);
                return null;
            }
            JsonArray authorsToParse = (JsonArray)responseJSON["response"]!;
            var authorsData = authorsToParse.Select(info => new KeyValuePair<string, (int?, string?)>
            (
                info!["id"]!.GetValue<int>().ToString(),
                (
                    info!["sex"] == null ? null : info!["sex"]!.GetValue<int>(),
                    info!["bdate"] == null ? null : info!["bdate"]!.GetValue<string>()
                )
            ));
            return new Dictionary<string, (int?, string?)>(authorsData);
        }
        // Выполняет поиск комментариев для объекта Post
        private async Task GetCommentsOfPostAsync(Post post, bool withAuthorsInfo, int count = -1, bool ignoreEmptyText = true)
        {
            List<Post.Comment> comments = new List<Post.Comment>();
            int startIndex = 0;
            int commentsParsed = 0;
            bool enough = false;            
            while (!enough)
            {
                string response = await apiProvider.ExecuteWallGetCommentsAsync(post.Author.Id, post.Id, startIndex, VKAPIProvider.maxCommentsPerRequest);
                startIndex += VKAPIProvider.maxCommentsPerRequest;               
                IEnumerable<Post.Comment>? thisRequestComments = ParseJSONAsComments(response);                
                if (thisRequestComments == null)
                {
                    post.Comments = null;
                    return;
                }
                if (thisRequestComments.Count() < VKAPIProvider.maxCommentsPerRequest) // Дошли до конца комментариев
                {
                    enough = true;
                }
                if (ignoreEmptyText)
                    thisRequestComments = thisRequestComments.Where(c => !string.IsNullOrWhiteSpace(c.Text));
                commentsParsed += thisRequestComments.Count();                
                if (count != -1 && commentsParsed >= count) // Задан и превышен лимит на число комментариев 
                {
                    thisRequestComments = thisRequestComments.SkipLast(commentsParsed - count);
                    enough = true;
                }
                thisRequestComments = thisRequestComments.ToArray();
                comments.AddRange(thisRequestComments);
            }
            if (withAuthorsInfo)
                await GetCommentsAuthorsInfoAsync(comments);
            post.Comments = comments.ToArray();
        }        
        private async Task GetPostsAuthorsInfoAsync(IEnumerable<Post> posts)
        {
            string authorIDs = string.Join(',', posts.Select(post => post.Author.Id));
            var authorsData = await GetAuthorsDataAsync(authorIDs);
            if (authorsData == null)
                return;
            foreach (Post post in posts)
            {
                if (int.Parse(post.Author.Id) > 0)
                {
                    Sex? cursex = authorsData[post.Author.Id].sex == null || authorsData[post.Author.Id].sex == 0 ? null : (authorsData[post.Author.Id].sex == 1 ? Sex.Female : Sex.Male);
                    DateOnly? curdate = authorsData[post.Author.Id].bdate == null || authorsData[post.Author.Id].bdate!.Split('.').Length == 2 ? null : DateOnly.ParseExact(authorsData[post.Author.Id].bdate!, "d.M.yyyy");
                    post.Author.Sex = cursex;
                    post.Author.BirthDate = curdate;
                }
            }
        }
        private async Task GetCommentsAuthorsInfoAsync(IEnumerable<Post.Comment> comments)
        {
            string authorIDs = string.Join(',', comments.Select(comment => comment.Author.Id));
            var authorsData = await GetAuthorsDataAsync(authorIDs);
            if (authorsData == null)
                return;
            foreach (Post.Comment comment in comments)
            {
                if (int.Parse(comment.Author.Id) > 0)
                {
                    Sex? cursex = authorsData[comment.Author.Id].sex == null || authorsData[comment.Author.Id].sex == 0 ? null : (authorsData[comment.Author.Id].sex == 1 ? Sex.Female : Sex.Male);
                    DateOnly? curdate = authorsData[comment.Author.Id].bdate == null || authorsData[comment.Author.Id].bdate!.Split('.').Length == 2 ? null : DateOnly.ParseExact(authorsData[comment.Author.Id].bdate!, "d.M.yyyy");
                    comment.Author.Sex = cursex;
                    comment.Author.BirthDate = curdate;
                }
            }
        }    
    }
}