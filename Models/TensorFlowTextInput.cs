using Microsoft.ML.Data;
using PublicOpinionViewer.Workers;

namespace PublicOpinionViewer.Models;

public class TensorFlowTextInput
{
    [VectorType(50)]
    [ColumnName(TensorFlowWorker.Config.inputLayerTFName)]
    public float[] Tokens { get; set; }

    public TensorFlowTextInput(float[] tokens)
    {
        Tokens = tokens;
    }
}
