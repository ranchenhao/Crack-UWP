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
    public class Segmentation
    {
        private crack_7Model model = null;
        private string ModelFilename = "crack_7.onnx";
        private DataProcess dataProcess = new DataProcess();

        private Stopwatch TimeRecorder = new Stopwatch();
        public string info_text;

        public Segmentation()
        {
            info_text = "Initializing...";
        }

        public async Task LoadModelAsync()
        {
            try
            {
                TimeRecorder = Stopwatch.StartNew();

                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri($"ms-appx:///Assets/{ModelFilename}"));
                model = await crack_7Model.CreateFromStreamAsync(modelFile);

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

                    crack_7Input inputData = new crack_7Input();
                    // Transer input image to required size and type
                    SoftwareBitmap resizedBitmap = await dataProcess.ResizeBitmap(sfbmp, 512, 512);
                    inputData.input_1 = await dataProcess.ConvertBitmapToTensorFloat(resizedBitmap);

                    // Segment
                    var output = await model.EvaluateAsync(inputData).ConfigureAwait(false);
                    // result = await dataProcess.ConvertTensorFloatToBitmap(output.conv2d_21);
                    result = await dataProcess.AugmentDisplay(resizedBitmap, output.conv2d_21);
                    //result = SoftwareBitmap.Copy(resizedBitmap);
                    TimeRecorder.Stop();

                    string message = $"({DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second})" +
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
    }
}
