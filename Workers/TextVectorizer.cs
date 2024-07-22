using PublicOpinionViewer.Models;

namespace PublicOpinionViewer.Workers
{
    public class TextVectorizer
    {
        // Ссылка на словарь индексов слов, сформированный при обучении моделей в models.py
        private const string indexVectorsVocabPath = @"Vectorization\TokensIndexVocabulary";
        private Dictionary<string, float> indexTokens;
        public TextVectorizer() 
        {
            indexTokens = new Dictionary<string, float>();
            foreach (string line in File.ReadAllLines(indexVectorsVocabPath))
            {
                string[] tokenAndNumberPair = line.Split(' ');
                indexTokens[tokenAndNumberPair[0]] = float.Parse(tokenAndNumberPair[1]);
            }
        }
        // Осуществляет приведение вектора текста к фиксированной длине (обрезать либо дополнить нулями справа)
        private void ApplyPadding(ref float[] vector, int newLength)
        {
            Array.Resize(ref vector, newLength);
        }
        // Векторизует текст согласно словарю
        private float[] VectorizeSingleText(string text, Dictionary<string, float> vocabulary, char separator = ' ')
        {
            string[] textTokens = text.Split(separator);
            float[] tokens = new float[textTokens.Length];
            for(int i = 0; i < textTokens.Length; i++) 
            {
                if (vocabulary.ContainsKey(textTokens[i]))
                    tokens[i] = vocabulary[textTokens[i]];
                else
                    tokens[i] = 1;
            }
            return tokens;
        }
        /// <summary>
        /// Приводит корпус токенизированных текстов к набору векторов фиксированной длины
        /// </summary>
        /// <param name="texts">Токенизированные тексты</param>
        /// <returns>Вектора фиксированной длины для подачи на входы нейронной сети</returns>
        public TensorFlowTextInput[] Vectorize(string[] texts)
        {
            TensorFlowTextInput[] encodedTexts = new TensorFlowTextInput[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                float[] tokens = VectorizeSingleText(texts[i], indexTokens);
                ApplyPadding(ref tokens, 50);
                encodedTexts[i] = new TensorFlowTextInput(tokens);
            }
            return encodedTexts;
        }
    }
}
