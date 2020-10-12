using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.AI.MachineLearning;

namespace Crack_UWP
{
    public class Classification
    {
        private crack_class_7Model model = null;
        private string ModelFilename = "crack_class_7.onnx";
        private DataProcess dataProcess = new DataProcess();

        private Stopwatch TimeRecorder = new Stopwatch();
        public string info_text;
        private TensorFloat classResult;

        public Classification()
        {
            info_text = "Initializing...";
            classResult = null;
        }

        public async Task LoadModelAsync()
        {
            try
            {
                TimeRecorder = Stopwatch.StartNew();

                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri($"ms-appx:///Assets/{ModelFilename}"));
                model = await crack_class_7Model.CreateFromStreamAsync(modelFile);

                TimeRecorder.Stop();
                ModifyText($"Loaded {ModelFilename}: Elapsed time: {TimeRecorder.ElapsedMilliseconds} ms\nPredicting...");
            }
            catch (Exception ex)
            {
                ModifyText($"error: {ex.Message}");
                model = null;
            }
        }

        public async Task<SoftwareBitmap> PredictImageAsync(SoftwareBitmap sfbmp)
        {
            SoftwareBitmap result = null;
            if (sfbmp != null)
            {
                try
                {
                    TimeRecorder.Restart();

                    crack_class_7Input inputData = new crack_class_7Input();
                    // Transer input image to required size and type
                    SoftwareBitmap resizedBitmap = await dataProcess.ResizeBitmap(sfbmp, 672, 672);
                    inputData.input_1 = dataProcess.ConvertBitmapToTensorFloat(resizedBitmap);

                    // Segment
                    var prediction = await model.EvaluateAsync(inputData).ConfigureAwait(false);
                    classResult = prediction.flatten;
                    result = dataProcess.AugmentClassDisplay(resizedBitmap, classResult);
                    TimeRecorder.Stop();

                    string message = "";
                    var res = prediction.flatten.GetAsVectorView();
                    for (int i = 0; i < res.Count; i++)
                    {
                        if (res.ElementAt(i) > 0)
                            message += "True ";
                        else
                            message += "False ";
                    }
                    message += "\n";
                    message += $"({DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second})" +
                        $" Evaluation took {TimeRecorder.ElapsedMilliseconds}ms\n";

                    message = message.Replace("\\n", "\n");

                    ModifyText(message);
                }
                catch (Exception ex)
                {
                    var err_message = $"error: {ex.Message}";
                    ModifyText(err_message);
                }
            }
            return result;
        }

        private void ModifyText(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
            info_text = text;
        }

        public TensorFloat GetResult()
        {
            return classResult;
        }
    }
}