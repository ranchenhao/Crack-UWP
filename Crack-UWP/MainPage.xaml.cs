using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.AI.MachineLearning;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Crack_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            //segmentation = new Segmentation();
            //infoText.Text = segmentation.info_text;
            classification = new Classification();
            segmentation = new Segmentation();
            infoText.Text = classification.info_text;
        }

        private Stopwatch TimeRecorder = new Stopwatch();
        private DataProcess dataProcess = new DataProcess();
        private SoftwareBitmap oriImage;
        private TensorFloat classResult;

        public Classification classification;
        public Segmentation segmentation;

        public async void Image_Loaded(object sender, RoutedEventArgs e)
        {
            StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/307.png"));
            

            using (IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file
                oriImage = await decoder.GetSoftwareBitmapAsync();
            }

            // oriImage = dataProcess.CropBitmap(oriImage, 0, 200, 2196, 2196);
            oriImage = await dataProcess.ResizeBitmap(oriImage, 672, 672);
            // Display original image
            var displayBitmap = SoftwareBitmap.Convert(oriImage, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayBitmap);
            img.Source = source;

            // Predict image
            await classification.LoadModelAsync();
            infoText.Text = classification.info_text;
            var result = await classification.PredictImageAsync(oriImage);
            infoText.Text = classification.info_text;
            classResult = classification.GetResult();

            // Draw segmented image
            displayBitmap = SoftwareBitmap.Convert(result, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            source.Dispose();
            source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayBitmap);
            img.Source = source;
        }

        public async void Button_Seg(object sender, RoutedEventArgs e)
        {
            if (classResult == null)
            {
                infoText.Text = "No classification result";
                return;
            }

            TimeRecorder = Stopwatch.StartNew();

            TensorFloat[] segResult = new TensorFloat[9];
            var result = classResult.GetAsVectorView();
            uint block_width = (uint)oriImage.PixelWidth / 3;
            uint block_height= (uint)oriImage.PixelHeight / 3;

            infoText.Text = "Loading Segmentation Model...";
            await segmentation.LoadModelAsync();
            infoText.Text = "Predicting...";
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] > 0)
                {
                    uint start_x = (uint)(i / 3) * block_height;
                    uint start_y = (uint)(i % 3) * block_width;

                    SoftwareBitmap cropImage = dataProcess.CropBitmap(oriImage, start_x, start_y, block_width, block_height);

                    segResult[i] = await segmentation.PredictImageAsync(cropImage);

                    //SoftwareBitmap resImage = dataProcess.ConvertTensorFloatToBitmap(segResult[i]);
                    //var _displayBitmap = SoftwareBitmap.Convert(resImage, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    //var _source = new SoftwareBitmapSource();
                    //await _source.SetBitmapAsync(_displayBitmap);
                    //img.Source = _source;
                    //await Task.Delay(1000);
                }
            }

            infoText.Text = "Rendering...";
            SoftwareBitmap segImage = dataProcess.MergeSegDisplay(oriImage, segResult, 3);

            var displayBitmap = SoftwareBitmap.Convert(segImage, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayBitmap);
            img.Source = source;

            TimeRecorder.Stop();
            infoText.Text = $"Elapsed time for segmentation: {TimeRecorder.ElapsedMilliseconds} ms";
        }
    }
}
