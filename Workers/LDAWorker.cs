using Microsoft.ML;
using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public class LDAWorker : IMLWorker
    {
        public MLContext Context { get; init; }
        public LDAWorker()
        {
            Context = new MLContext();
        }
        public Topic[] FindTopics(string[] texts, int topicsNum, int termsPerTopic)
        {
            LDATextInput[] textInputs = texts.Select(t => new LDATextInput(t)).ToArray();
            var pipeline = Context.Transforms.Text.TokenizeIntoWords("Tokens", "Text")
                .Append(Context.Transforms.Conversion.MapValueToKey("Tokens"))
                .Append(Context.Transforms.Text.ProduceNgrams("Tokens", ngramLength: 2, useAllLengths: false))
                .Append(Context.Transforms.Text.LatentDirichletAllocation("Topics", "Tokens", numberOfTopics: topicsNum, numberOfSummaryTermsPerTopic: termsPerTopic));
            var transformer = pipeline.Fit(Context.Data.LoadFromEnumerable(textInputs));
            var parameters = transformer.LastTransformer.GetLdaDetails(0);

            Topic[] topics = new Topic[topicsNum];
            for (int i = 0; i < topicsNum; i++)
            {
                topics[i] = new Topic();
                foreach (var score in parameters.WordScoresPerTopic[i])
                {
                    topics[i].Terms.Add(score.Word, score.Score);
                }
            }
            return topics;
        }
    }
}
