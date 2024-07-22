using Microsoft.ML;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public class SKLearnWorker : IMLWorker, IMLPredictor
    {             
        public enum ModelType
        {
            NaiveBayes = 7,
            LogisticRegression,
            SVM
        }
        public enum VectorizationMode
        {
            BagOfWords = 1,
            TfIdf
        }
        public enum PreprocessingMode
        {
            Stemming = 1,
            Lemmatization
        }
        private const string inputLayerName = "text_input";
        public ModelType Type { get; init; }
        public VectorizationMode Vectorization { get; init; }
        public PreprocessingMode Preprocessing { get; init; }
        public MLContext Context { get; init; }
        private static Dictionary<(ModelType, VectorizationMode, PreprocessingMode), string> modelsPaths = new Dictionary<(ModelType, VectorizationMode, PreprocessingMode), string>()
        {
            { (ModelType.NaiveBayes, VectorizationMode.BagOfWords, PreprocessingMode.Stemming), @"MLModels\SKLearn\NaiveBayes\NaiveBayesBoWStemmed.onnx" },
            { (ModelType.NaiveBayes, VectorizationMode.TfIdf, PreprocessingMode.Stemming), @"MLModels\SKLearn\NaiveBayes\NaiveBayesTfIdfStemmed.onnx" },
            { (ModelType.NaiveBayes, VectorizationMode.BagOfWords, PreprocessingMode.Lemmatization), @"MLModels\SKLearn\NaiveBayes\NaiveBayesBoWLemmatized.onnx" },
            { (ModelType.NaiveBayes, VectorizationMode.TfIdf, PreprocessingMode.Lemmatization), @"MLModels\SKLearn\NaiveBayes\NaiveBayesTfIdfLemmatized.onnx" },
            { (ModelType.LogisticRegression, VectorizationMode.BagOfWords, PreprocessingMode.Stemming), @"MLModels\SKLearn\LogisticRegression\LogisticRegressionBoWStemmed.onnx" },
            { (ModelType.LogisticRegression, VectorizationMode.TfIdf, PreprocessingMode.Stemming), @"MLModels\SKLearn\LogisticRegression\LogisticRegressionTfIdfStemmed.onnx" },
            { (ModelType.LogisticRegression, VectorizationMode.BagOfWords, PreprocessingMode.Lemmatization), @"MLModels\SKLearn\LogisticRegression\LogisticRegressionBoWLemmatized.onnx" },
            { (ModelType.LogisticRegression, VectorizationMode.TfIdf, PreprocessingMode.Lemmatization), @"MLModels\SKLearn\LogisticRegression\LogisticRegressionTfIdfLemmatized.onnx" },
            { (ModelType.SVM, VectorizationMode.BagOfWords, PreprocessingMode.Stemming), @"MLModels\SKLearn\SVM\SVMBoWStemmed.onnx" },
            { (ModelType.SVM, VectorizationMode.TfIdf, PreprocessingMode.Stemming), @"MLModels\SKLearn\SVM\SVMTfIdfStemmed.onnx" },
            { (ModelType.SVM, VectorizationMode.BagOfWords, PreprocessingMode.Lemmatization), @"MLModels\SKLearn\SVM\SVMBoWLemmatized.onnx" },
            { (ModelType.SVM, VectorizationMode.TfIdf, PreprocessingMode.Lemmatization), @"MLModels\SKLearn\SVM\SVMTfIdfLemmatized.onnx" },
        };
        public bool WithTonalityChances { get; init; }
        public SKLearnWorker(ModelType modelType, VectorizationMode vectorizationMode, PreprocessingMode preprocessingMode)
        {
            Context = new MLContext();
            Type = modelType;
            Vectorization = vectorizationMode;
            Preprocessing = preprocessingMode;
            WithTonalityChances = modelType != ModelType.SVM ? true : false;
        }
        public Sentiment[] Predict(string[] texts)
        {            
            Sentiment[] sentiments = new Sentiment[texts.Length];
            string modelPath = modelsPaths[(Type, Vectorization, Preprocessing)];
            using InferenceSession session = new InferenceSession(modelPath);
            
            for (int i = 0; i < texts.Length; i++)
            {
                var inputTensor = new DenseTensor<string>(new[] { texts[i] }, [1, 1]);
                var inputValue = NamedOnnxValue.CreateFromTensor(inputLayerName, inputTensor);
                var inputValuesContainer = new List<NamedOnnxValue>
                {
                    inputValue
                };                
                using var outputs = session.Run(inputValuesContainer);
                var outputLabel = outputs.ElementAt(0).AsTensor<long>();
                var outputProbability = outputs.ElementAt(1).AsEnumerable<NamedOnnxValue>().First().AsDictionary<long, float>();
                sentiments[i] = new Sentiment() { 
                    TonalityChances = 
                        WithTonalityChances 
                            ? [outputProbability[0],
                            outputProbability[1], 
                            outputProbability[2]]
                            : null,
                    TonalityLabel = (Sentiment.Label)(int)outputLabel[0]
                };
            }
            return sentiments;
        }
    }
}
