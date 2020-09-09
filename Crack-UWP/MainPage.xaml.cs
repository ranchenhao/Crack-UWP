using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            segmentation = new Segmentation();
            infoText.Text = segmentation.info_text;
        }

        public Segmentation segmentation;

        public async void Image_Loaded(object sender, RoutedEventArgs e)
        {
            StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/307.png"));
            SoftwareBitmap softwareBitmap;

            using (IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            }

            // Display original image
            Image img = sender as Image;
            var displayBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayBitmap);
            img.Source = source;

            // Predict image
            await segmentation.LoadModelAsync();
            infoText.Text = segmentation.info_text;
            var result = await segmentation.PredictImageAsync(softwareBitmap);
            infoText.Text = segmentation.info_text;

            // Draw segmented image
            displayBitmap = SoftwareBitmap.Convert(result, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            source.Dispose();
            source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayBitmap);
            img.Source = source;
        }
    }
}
