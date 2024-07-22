using Microsoft.ML.Data;
using PublicOpinionViewer.Workers;

namespace PublicOpinionViewer.Models
{
    public class Sentiment
    {
        public enum Label
        {
            Negative,
            Neutral,
            Positive
        }
        [VectorType(3)]
        [ColumnName(TensorFlowWorker.Config.outputLayerTFName)]
        public float[]? TonalityChances { get; set; }
        [NoColumn]
        public Label TonalityLabel { get; set; }
        [NoColumn]
        public float? TonalityChance => GetTonalityChange(TonalityLabel);
        public float? GetTonalityChange(Label label)
        {
            if (TonalityChances == null)
                return null;
            return TonalityChances[(int)TonalityLabel!];
        }     
    }
}

