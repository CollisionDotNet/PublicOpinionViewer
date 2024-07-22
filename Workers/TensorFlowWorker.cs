using Microsoft.ML.Transforms;
using Microsoft.ML;
using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public class TensorFlowWorker : IMLWorker, IMLPredictor
    {
        public static class Config
        {
            public const string inputLayerTFName = "serving_default_input_1";
            public const string outputLayerTFName = "StatefulPartitionedCall";
        }
        public enum ModelType
        {
            MLPCustomEmbedding = 1,
            MLPWord2Vec300,
            CNN,
            SimpleRNN,
            LSTM,
            GRU
        }
        public enum PreprocessingMode
        {
            Stemming = 1,
            Lemmatization
        }
        public ModelType Type { get; init; }
        public PreprocessingMode Preprocessing { get; init; }
        public MLContext Context { get; init; }
        private TensorFlowModel Model { get; init; }
        private static Dictionary<(ModelType, PreprocessingMode), string> modelsPaths = new Dictionary<(ModelType, PreprocessingMode), string>()
        {
            { (ModelType.MLPCustomEmbedding, PreprocessingMode.Stemming), @"MLModels\Keras\MLPCustomEmbeddingStemmed" },
            { (ModelType.MLPCustomEmbedding, PreprocessingMode.Lemmatization), @"MLModels\Keras\MLPCustomEmbeddingLemmatized" },
            { (ModelType.MLPWord2Vec300, PreprocessingMode.Stemming), @"MLModels\Keras\MLPWord2Vec300Stemmed" },
            { (ModelType.MLPWord2Vec300, PreprocessingMode.Lemmatization), @"MLModels\Keras\MLPWord2Vec300Lemmatized" },
            { (ModelType.CNN, PreprocessingMode.Stemming), @"MLModels\Keras\CNNStemmed" },
            { (ModelType.CNN, PreprocessingMode.Lemmatization), @"MLModels\Keras\CNNLemmatized" },
            { (ModelType.SimpleRNN, PreprocessingMode.Stemming), @"MLModels\Keras\SimpleRNNStemmed" },
            { (ModelType.SimpleRNN, PreprocessingMode.Lemmatization), @"MLModels\Keras\SimpleRNNLemmatized" },
            { (ModelType.LSTM, PreprocessingMode.Stemming), @"MLModels\Keras\LSTMStemmed" },
            { (ModelType.LSTM, PreprocessingMode.Lemmatization), @"MLModels\Keras\LSTMLemmatized" },
            { (ModelType.GRU, PreprocessingMode.Stemming), @"MLModels\Keras\GRUStemmed" },
            { (ModelType.GRU, PreprocessingMode.Lemmatization), @"MLModels\Keras\GRULemmatized" },
        };
        public TensorFlowWorker(ModelType modelType, PreprocessingMode preprocessingMode)
        {
            Context = new MLContext();
            Type = modelType;
            Preprocessing = preprocessingMode;
            Model = Context.Model.LoadTensorFlowModel(modelsPaths[(Type, Preprocessing)]);
        }
        public Sentiment[] Predict(string[] texts)
        {
            TextVectorizer textVectorizer = new TextVectorizer();
            TensorFlowTextInput[] encodedTexts = textVectorizer.Vectorize(texts);
            var estimator = Model.ScoreTensorFlowModel(Config.outputLayerTFName, Config.inputLayerTFName);
            var emptyDataView = Context.Data.LoadFromEnumerable(new List<TensorFlowTextInput>());
            var transformer = estimator.Fit(emptyDataView);
            var engine = Context.Model.CreatePredictionEngine<TensorFlowTextInput, Sentiment>(transformer);
            Sentiment[] sentiments = encodedTexts.Select(engine.Predict).ToArray();
            for (int i = 0; i < sentiments.Length; i++)
            {
                sentiments[i].TonalityLabel = (Sentiment.Label)Array.IndexOf(sentiments[i].TonalityChances!, sentiments[i].TonalityChances!.Max()); // После Predict TonalityChances уже определены
            }
            return sentiments;
        }
    }
}
