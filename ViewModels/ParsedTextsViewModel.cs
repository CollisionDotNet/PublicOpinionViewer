using PublicOpinionViewer.Models;
using PublicOpinionViewer.Workers;
using System.Collections.ObjectModel;

namespace PublicOpinionViewer.ViewModels
{
    public class ParsedTextsViewModel
    {       
        public class ParsedText
        {
            private const int shortTextMaxLength = 200;
            public string Text { get; init; }
            public string ShortedText { get; init; }
            public ParsedText(SentimentText parsedText)
            {
                Text = parsedText.Text;
                ShortedText = StringExtensions.ShortenText(parsedText.Text, shortTextMaxLength);
            }           
        }
        public class AuthoredText : ParsedText
        {
            public string? AuthorSex { get; init; }
            public string? AuthorAge { get; init; }
            public AuthoredText(Post parsedPost) : base(parsedPost)
            {
                AuthorSex = parsedPost.Author.Sex == null ? "не указан" : parsedPost.Author.Sex.ToString();
                AuthorAge = parsedPost.Author.Age == null ? "не указан" : parsedPost.Author.Age.ToString();
            }
            public AuthoredText(Post.Comment parsedComment) : base(parsedComment)
            {
                AuthorSex = parsedComment.Author.Sex == null ? "не указан" : parsedComment.Author.Sex.ToString();
                AuthorAge = parsedComment.Author.Age == null ? "не указан" : parsedComment.Author.Age.ToString();
            }
        }
        public class ParsedPost : AuthoredText
        {
            public string Link { get; init; }
            public AuthoredText[]? Comments { get; init; }
            public ParsedPost(Post analyzedPost) : base(analyzedPost)
            {
                Link = $"https://vk.com/wall{analyzedPost.Author.Id}_{analyzedPost.Id}";
                if (analyzedPost.Comments != null)
                    Comments = analyzedPost.Comments.Select(c => new AuthoredText(c)).ToArray();
            }
        }
        public ReadOnlyCollection<ParsedText>? ParsedTexts { get; init; }
        public ReadOnlyCollection<ParsedPost>? ParsedPosts { get; init; }
        public ReadOnlyCollection<Topic>? Topics { get; init; }
        public ParsedTextsViewModel(IEnumerable<SentimentText> parsedTexts)
        {
            if(parsedTexts is IEnumerable<Post> parsedPosts)
            {
                ParsedPosts = parsedPosts.Select(p => new ParsedPost(p)).ToList().AsReadOnly();
            }
            else
            {
                ParsedTexts = parsedTexts.Select(t => new ParsedText(t)).ToList().AsReadOnly();
            }                     
        }
        public ParsedTextsViewModel(IEnumerable<SentimentText> parsedTexts, IEnumerable<Topic> topics)
        {
            if (parsedTexts is IEnumerable<Post> parsedPosts)
            {
                ParsedPosts = parsedPosts.Select(p => new ParsedPost(p)).ToList().AsReadOnly();
            }
            else
            {
                ParsedTexts = parsedTexts.Select(t => new ParsedText(t)).ToList().AsReadOnly();
            }
            Topics = topics.ToList().AsReadOnly();
        }
    }
}
