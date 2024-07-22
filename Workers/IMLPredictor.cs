using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public interface IMLPredictor
    {
        public Sentiment[] Predict(string[] texts);
    }
}
