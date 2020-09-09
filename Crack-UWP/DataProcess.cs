using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Crack_UWP
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public class DataProcess
    {
        public async Task<SoftwareBitmap> ResizeBitmap(SoftwareBitmap input, uint w, uint h)
        {
            SoftwareBitmap resizedBitMap;

            // Resize image
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
                encoder.SetSoftwareBitmap(input);
                encoder.BitmapTransform.ScaledWidth = w;
                encoder.BitmapTransform.ScaledHeight = h;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                await encoder.FlushAsync();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                resizedBitMap = await decoder.GetSoftwareBitmapAsync(input.BitmapPixelFormat, input.BitmapAlphaMode);
            }

            return resizedBitMap;
        }


        // Convert BGRA softwarebitmap to tensorfloat
        public async Task<TensorFloat> ConvertBitmapToTensorFloat(SoftwareBitmap input)
        {
            var width = input.PixelWidth;
            var height = input.PixelHeight;
            float[] data = new float[width * height * 3];

            using (var buffer = input.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataInBytes;
                        uint capacity;
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                        // The channels of image stored in buffer is in order of BGRA-BGRA-BGRA-BGRA. 
                        // Then we transform it to the order of BBBBB....GGGGG....RRRR....AAAA(dropped) 
                        BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                        int startInd = bufferLayout.StartIndex;
                        for (uint i = 0; i < capacity; i += 4)
                        {
                            uint pixelInd = i / 4;
                            data[pixelInd * 3] = (float)dataInBytes[startInd + i + 2];
                            data[pixelInd * 3 + 1] = (float)dataInBytes[startInd + i + 1];
                            data[pixelInd * 3 + 2] = (float)dataInBytes[startInd + i];

                            /*
                            uint pixelInd = i / 4;
                            float grayValue = (float)(dataInBytes[startInd + i] * 0.114 + dataInBytes[startInd + i + 1] * 0.587 + dataInBytes[startInd + i + 2] * 0.299);
                            data[pixelInd * 3] = data[pixelInd * 3 + 1] = data[pixelInd * 3 + 2] = grayValue;
                            */
                        }
                    }
                }
            }

            long[] shape = { 1, width, height, 3 };
            var tensor = TensorFloat.CreateFromArray(shape, data);

            return tensor;
        }

        public async Task<SoftwareBitmap> ConvertTensorFloatToBitmap(TensorFloat input)
        {
            int w = (int)input.Shape[1];
            int h = (int)input.Shape[2];
            var data = input.GetAsVectorView();
            int size = data.Count;
            byte[] dataInBytes = new byte[4 * size];

            byte alpha = 255; // channel a of bitmap
            for (int i = 0; i < size; i++)
            {
                float temp = data[i];
                byte pixelValue = data[i] > 0.5 ? (byte)0 : (byte)255;
                // pixelValue = 0;
                dataInBytes[i * 4 + 0] = dataInBytes[i * 4 + 1] = dataInBytes[i * 4 + 2] = pixelValue;
                dataInBytes[i * 4 + 3] = alpha;
            }
            SoftwareBitmap res = SoftwareBitmap.CreateCopyFromBuffer(dataInBytes.AsBuffer(), 
                BitmapPixelFormat.Bgra8, w, h);
            return res;
        }

        public async Task<SoftwareBitmap> AugmentDisplay(SoftwareBitmap ori, SoftwareBitmap seg)
        {
            SoftwareBitmap res = null;
            return res;
        }

        // ori here has the same size as seg, i.e. 512 * 512
        public async Task<SoftwareBitmap> AugmentDisplay(SoftwareBitmap ori, TensorFloat seg)
        {
            int w = (int)seg.Shape[1];
            int h = (int)seg.Shape[2];
            var data = seg.GetAsVectorView();
            int size = data.Count;
            SoftwareBitmap res = SoftwareBitmap.Copy(ori);

            using (var buffer = res.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataInBytes;
                        uint capacity;

                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
                        if (size * 4 != capacity)
                        {
                            return null;
                        }

                            // Add a red mask to the crack
                            BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                        int startInd = bufferLayout.StartIndex;
                        float alpha = 0.3f;
                        for (int i = 0; i < size; i++)
                        {
                            if (data[i] < 0.5)
                            {
                                dataInBytes[startInd + i * 4] = (byte)(dataInBytes[startInd + i * 4] * alpha);
                                dataInBytes[startInd + i * 4 + 1] = (byte)(dataInBytes[startInd + i * 4 + 1] * alpha);
                                dataInBytes[startInd + i * 4 + 2] = (byte)(dataInBytes[startInd + i * 4] * alpha + 255 * alpha);
                            }
                        }
                    }
                }
            }

            return res;
        }
    }
}
