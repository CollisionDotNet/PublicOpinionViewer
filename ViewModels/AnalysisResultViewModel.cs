using PublicOpinionViewer.Models;
using PublicOpinionViewer.Workers;
using System.Collections.ObjectModel;
using static PublicOpinionViewer.ViewModels.ParsedTextsViewModel;

namespace PublicOpinionViewer.ViewModels
{
    public class AnalysisResultViewModel
    {
        public class AnalyzedText
        {
            private const int shortTextMaxLength = 200;
            public string Text { get; init; }
            public string ShortedText { get; init; }
            public Sentiment.Label Tonality { get; init; }
            public float? TonalityChance { get; init; }
            public string IconName { get; init; }
            private static Dictionary<Sentiment.Label, string> sentimentIconNames = new Dictionary<Sentiment.Label, string>()
            {
                { Sentiment.Label.Negative, "negative_icon.png" },
                { Sentiment.Label.Neutral, "neutral_icon.png" },
                { Sentiment.Label.Positive, "positive_icon.png" }
            };
            public AnalyzedText(SentimentText analyzedText)
            {
                if (analyzedText.Sentiment == null)
                    throw new ArgumentNullException("Text was not analyzed!");
                Text = analyzedText.Text;
                ShortedText = StringExtensions.ShortenText(analyzedText.Text, shortTextMaxLength);
                TonalityChance = analyzedText.Sentiment.TonalityChance;
                Tonality = analyzedText.Sentiment.TonalityLabel;
                IconName = sentimentIconNames[Tonality];
            }
        }
        public class AnalyzedAuthoredText : AnalyzedText
        {
            public string? AuthorSex { get; init; }
            public string? AuthorAge { get; init; }
            public AnalyzedAuthoredText(Post parsedPost) : base(parsedPost)
            {
                AuthorSex = parsedPost.Author.Sex == null ? "не указан" : (parsedPost.Author.Sex == Sex.Male ? "Мужской" : "Женский");
                AuthorAge = parsedPost.Author.Age == null ? "не указан" : parsedPost.Author.Age.ToString();
            }
            public AnalyzedAuthoredText(Post.Comment parsedComment) : base(parsedComment)
            {
                AuthorSex = parsedComment.Author.Sex == null ? "не указан" : (parsedComment.Author.Sex == Sex.Male ? "Мужской" : "Женский");
                AuthorAge = parsedComment.Author.Age == null ? "не указан" : parsedComment.Author.Age.ToString();
            }
        }
        public class AnalyzedPost : AnalyzedAuthoredText
        {
            public string Link { get; init; }
            public AnalyzedAuthoredText[]? Comments { get; init; }
            public AnalyzedPost(Post analyzedPost) : base(analyzedPost)
            {
                Link = $"https://vk.com/wall{analyzedPost.Author.Id}_{analyzedPost.Id}";
                if(analyzedPost.Comments != null)
                    Comments = analyzedPost.Comments.Select(c => new AnalyzedAuthoredText(c)).ToArray();
            }            
        }       
        public ReadOnlyCollection<AnalyzedText>? AnalyzedTexts { get; init; }
        public ReadOnlyCollection<AnalyzedPost>? AnalyzedPosts { get; init; }
        public AnalysisResultViewModel(IEnumerable<SentimentText> analyzedTexts)
        {
            if (analyzedTexts is IEnumerable<Post> analyzedPosts)
            {
                AnalyzedPosts = analyzedPosts.Select(p => new AnalyzedPost(p)).ToList().AsReadOnly();
            }
            else
            {
                AnalyzedTexts = analyzedTexts.Select(t => new AnalyzedText(t)).ToList().AsReadOnly();
            }
        }       
    }
}
