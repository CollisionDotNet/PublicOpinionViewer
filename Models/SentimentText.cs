namespace PublicOpinionViewer.Models
{
    public class SentimentText // Описывает текст, имеющий определенную тональность
    {
        public string Text { get; init; }
        public Sentiment? Sentiment { get; set; }
        public SentimentText(string text)
        {
            Text = text;
        }
    }
}
