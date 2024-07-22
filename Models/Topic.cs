namespace PublicOpinionViewer.Models
{
    public class Topic
    {
        public Dictionary<string, float> Terms { get; set; }
        public Topic() 
        { 
            Terms = new Dictionary<string, float>();
        }
    }
}
