namespace PublicOpinionViewer.Models
{
    public class LDATextInput
    {
        public string Text { get; init; }
        public LDATextInput(string text)
        {
            Text = text;
        }
    }
}
