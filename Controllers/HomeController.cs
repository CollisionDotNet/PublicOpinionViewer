using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using PublicOpinionViewer.Models;
using PublicOpinionViewer.ViewModels;
using PublicOpinionViewer.Workers;
using System.Text;

namespace PublicOpinionViewer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [ActionName("FoundTrends")]
        public async Task<IActionResult> FindTrendsAsync(int topicsnum, int termspertopic, int postspergrouptopic, bool withcommstopic, int commsperposttopic)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            VKAggregator aggregator = new VKAggregator();
            Post[]? posts;
            if (!withcommstopic)
                commsperposttopic = 0;
            else if (commsperposttopic == 0)
                commsperposttopic = -1;
            posts = await aggregator.GetPostsFromPopularGroupsAsync(postspergrouptopic, false, commsperposttopic);
            if (posts == null)
            {
                throw new NotImplementedException("Не удалось выгрузить посты из популярных групп!");
            }
            TextsSaveLoader.Save(posts, out string postsDir);
            IEnumerable<string> texts = posts.SelectMany(TextsSaveLoader.Flatten);            
            TextPreprocessor preprocessor = new TextPreprocessor(TextPreprocessor.Language.Russian);
            texts = await PreprocessWithLemmasAsync(preprocessor, texts, true);

            LDAWorker worker = new LDAWorker();
            Topic[] topics = worker.FindTopics(texts.ToArray(), topicsnum, termspertopic);
            return View("FoundTrendsView", topics);
        }
        [ActionName("Search")]
        public async Task<IActionResult> SearchAsync(string poststheme, int postscount, bool withauthorsinfo, bool withcomms, int commsperpost, bool withtopics)
        {
            if (!withcomms)
                commsperpost = 0;
            else if (commsperpost == 0)
                commsperpost = -1;
            VKAggregator aggregator = new VKAggregator();
            Post[]? posts = await aggregator.GetPostsByThemeAsync(poststheme, postscount, true, commsperpost);
            if (posts == null)
            {
                throw new NotImplementedException("Не удалось выгрузить посты по теме!");
            }            
            TextsSaveLoader.Save(posts, out string curSerssionDir);
            TempData["Topic"] = poststheme;
            TempData["TextsDirectory"] = curSerssionDir;
            if (withtopics)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                IEnumerable<string> texts = posts.Where(p => p.Comments != null).SelectMany(p => p.Comments!).Select(c => c.Text);
                TextPreprocessor preprocessor = new TextPreprocessor(TextPreprocessor.Language.Russian);
                texts = await PreprocessWithLemmasAsync(preprocessor, texts, true);

                LDAWorker worker = new LDAWorker();
                Topic[] topics = worker.FindTopics(texts.ToArray(), 5, 5);
                return View("ParsedTextsView", new ParsedTextsViewModel(posts, topics));
            }
            else           
                return View("ParsedTextsView", new ParsedTextsViewModel(posts));            
        }
        [ActionName("ProcessTexts")]
        public IActionResult ProcessTexts(IFormFile textsFile)
        {
            List<SentimentText> texts = new List<SentimentText>();
            using(StreamReader stream = new StreamReader(textsFile.OpenReadStream()))
            {
                while (stream.Peek() >= 0)
                    texts.Add(new SentimentText(stream.ReadLine()!));
            }               
            TextsSaveLoader.Save(texts, out string curSerssionDir);
            TempData["Topic"] = null;
            TempData["TextsDirectory"] = curSerssionDir;
            return View("ParsedTextsView", new ParsedTextsViewModel(texts));
        }
        [ActionName("GetSentiments")]
        public async Task<IActionResult> GetSentimentsAsync(string topic, int modeltype, int vectortype, int stemtype, int areposts)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            TempData["Topic"] = topic;
            string textsDir = (TempData["TextsDirectory"] as string)!;
            SentimentText[] texts = areposts == 1 ? TextsSaveLoader.LoadPosts(textsDir).ToArray() : TextsSaveLoader.LoadTexts(textsDir).ToArray();
            IEnumerable<string> textsStr = texts.Select(t => t.Text);
            TextPreprocessor preprocessor = new TextPreprocessor(TextPreprocessor.Language.Russian);
            if (stemtype == 1)
            {
                textsStr = PreprocessWithStems(preprocessor, textsStr, false);
            }
            else
            {
                textsStr = await PreprocessWithLemmasAsync(preprocessor, textsStr, false);
            }
            IMLPredictor classifier;
            Sentiment[] textsSentiments;
            if (modeltype > 6)
            {
                classifier = new SKLearnWorker((SKLearnWorker.ModelType)modeltype, (SKLearnWorker.VectorizationMode)vectortype, (SKLearnWorker.PreprocessingMode)stemtype);
                textsSentiments = classifier.Predict(textsStr.ToArray());
            }
            else
            {                
                classifier = new TensorFlowWorker((TensorFlowWorker.ModelType)modeltype, (TensorFlowWorker.PreprocessingMode)stemtype);
                textsSentiments = classifier.Predict(textsStr.ToArray());             
            }
            for (int i = 0; i < texts.Length; i++)
            {               
                texts[i].Sentiment = textsSentiments[i];
                if (texts[i] is Post post && post.Comments != null)
                {
                    IEnumerable<string> postCommentsTexts = post.Comments!.Select(c => c.Text);
                    if (stemtype == 1)
                    {
                        postCommentsTexts = PreprocessWithStems(preprocessor, textsStr, false);
                    }
                    else
                    {
                        postCommentsTexts = await PreprocessWithLemmasAsync(preprocessor, textsStr, false);
                    }
                    Sentiment[] postCommentsSentiments = classifier.Predict(postCommentsTexts.ToArray());
                    for (int j = 0; j < post.Comments!.Length; j++)
                    {
                        post.Comments![j].Sentiment = postCommentsSentiments[j];
                    }
                }
            }
            return View("AnalysisResultView", new AnalysisResultViewModel(texts));
        }
        //TO DO
        [HttpPost]
        public void SaveCorrectSentimentText(string text, int sentiment)
        {
            TextsSaveLoader.SaveNewMLExample(text, (Sentiment.Label)sentiment);
        }
        private IEnumerable<IEnumerable<string>> TokenizeTexts(TextPreprocessor preprocessor, IEnumerable<string> texts, bool ingoreEmptyTexts = false)
        {
            var textsTokens = texts.Select(preprocessor.Tokenize);
            textsTokens = textsTokens.Select(preprocessor.RemoveStopwords);
            if(ingoreEmptyTexts)
                textsTokens = textsTokens.Where(t => t.Any()); // Отсеиваем тексты, состоящие лишь из стоп-слов
            return textsTokens;
        }
        private IEnumerable<string> PreprocessWithStems(TextPreprocessor preprocessor, IEnumerable<string> texts, bool ingoreEmptyTexts = false)
        {
            var textsTokens = TokenizeTexts(preprocessor, texts, ingoreEmptyTexts);
            textsTokens = textsTokens.Select(preprocessor.FindStems);
            texts = textsTokens.Select(t => string.Join(' ', t));
            return texts;
        }
        private async Task<IEnumerable<string>> PreprocessWithLemmasAsync(TextPreprocessor preprocessor, IEnumerable<string> texts, bool ingoreEmptyTexts = false)
        {
            var textsTokens = TokenizeTexts(preprocessor, texts, ingoreEmptyTexts);            
            var allTexts = string.Join('|', textsTokens.Select(t => string.Join(' ', t)));            
            var allLemmas = await preprocessor.FindLemmasAsync(allTexts);
            texts = string.Join(" ", allLemmas).Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return texts;
        }
    }
}
