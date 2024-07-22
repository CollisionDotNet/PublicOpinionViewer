namespace PublicOpinionViewer.Models
{
    public enum MediaSource
    {
        VK
    }
    public class Post : SentimentText // Описывает пост и текстовые комментарии к нему
    {        
        public class Comment : SentimentText
        {
            public Author Author { get; init; }
            public Comment(string text, Author author) : base(text) 
            { 
                Author = author;
            }
        }       
        public MediaSource Source { get; init; }
        public string Id { get; init; }
        public Author Author { get; init; }
        public Comment[]? Comments { get; set; }        
        public Post(string id, string text, Author author, Comment[]? comments, MediaSource source) : base(text) 
        {
            Id = id;
            Author = author;
            Comments = comments;
            Source = source;                                            
        }
    }
}