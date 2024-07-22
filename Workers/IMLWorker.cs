using Microsoft.ML;

namespace PublicOpinionViewer.Workers
{
    public interface IMLWorker
    {
        public MLContext Context { get; init; }
    }
}
