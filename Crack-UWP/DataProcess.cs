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


        public SoftwareBitmap CropBitmap(SoftwareBitmap input, uint start_x, uint start_y, uint width, uint height)
        {
            uint w = (uint)input.PixelWidth;
            uint h = (uint)input.PixelHeight;
            start_x = start_x < 0 ? 0 : start_x;
            start_y = start_y < 0 ? 0 : start_y;
            if (w < start_x + height || h < start_y + width)
            {
                height = h - start_x;
                width = w - start_y;
            }

            SoftwareBitmap croppedBitMap;
            byte[] crop_data = new byte[width * height * 4];

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
                        for (long x = 0; x < height; x++)
                        {
                            for(long y = 0; y < width; y++)
                            {
                                long input_index = startInd + (start_x + x) * w * 4 + (start_y + y) * 4;
                                long crop_index = x * width * 4 + y * 4;
                                crop_data[crop_index + 0] = dataInBytes[input_index + 0];
                                crop_data[crop_index + 1] = dataInBytes[input_index + 1];
                                crop_data[crop_index + 2] = dataInBytes[input_index + 2];
                                crop_data[crop_index + 3] = dataInBytes[input_index + 3];
                            }
                        }
                    }
                }
            }

            croppedBitMap = SoftwareBitmap.CreateCopyFromBuffer(crop_data.AsBuffer(),
                BitmapPixelFormat.Bgra8, (int)width, (int)height);
            return croppedBitMap;
        }


        // Convert BGRA softwarebitmap to tensorfloat
        public TensorFloat ConvertBitmapToTensorFloat(SoftwareBitmap input, bool normalize = false)
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
                            int divisor = normalize ? 255 : 1;
                            data[pixelInd * 3] = (float)dataInBytes[startInd + i + 2] / divisor;
                            data[pixelInd * 3 + 1] = (float)dataInBytes[startInd + i + 1] / divisor;
                            data[pixelInd * 3 + 2] = (float)dataInBytes[startInd + i] / divisor;

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

        public SoftwareBitmap ConvertTensorFloatToBitmap(TensorFloat input)
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
                byte pixelValue = data[i] > 0.35 ? (byte)0 : (byte)255;
                // pixelValue = 0;
                dataInBytes[i * 4 + 0] = dataInBytes[i * 4 + 1] = dataInBytes[i * 4 + 2] = pixelValue;
                dataInBytes[i * 4 + 3] = alpha;
            }
            SoftwareBitmap res = SoftwareBitmap.CreateCopyFromBuffer(dataInBytes.AsBuffer(), 
                BitmapPixelFormat.Bgra8, w, h);
            return res;
        }


        // ori here has the same size as seg, i.e. 512 * 512
        public SoftwareBitmap AugmentSegDisplay(SoftwareBitmap ori, TensorFloat seg)
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
                                dataInBytes[startInd + i * 4 + 2] = (byte)(dataInBytes[startInd + i * 4 + 2] * alpha + 255 * alpha);
                            }
                        }
                    }
                }
            }

            return res;
        }


        public SoftwareBitmap MergeSegDisplay(SoftwareBitmap ori, TensorFloat[] segResult, int grid_size)
        {
            SoftwareBitmap result = SoftwareBitmap.Copy(ori);

            using (var buffer = result.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataInBytes;
                        uint capacity;

                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                        // Add a red mask to the crack
                        BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                        int startInd = bufferLayout.StartIndex;
                        float alpha = 0.3f;

                        int block_width = ori.PixelWidth / grid_size;
                        int block_height = ori.PixelHeight / grid_size;
                        for (int i = 0; i < segResult.Length; i++)
                        {
                            TensorFloat seg = segResult[i];
                            if (seg == null)
                                continue;
                            var seg_data = seg.GetAsVectorView();

                            int row = i / grid_size * block_height;
                            int col = i % grid_size * block_width;


                            for (int x = 0; x < block_height; x++)
                            {
                                for (int y = 0; y < block_width; y++)
                                {
                                    int ori_index = startInd + (row + x) * ori.PixelWidth * 4 + (col + y) * 4;
                                    int seg_index = x * block_width + y;
                                    if (seg_data[seg_index] > 0.35)
                                    {
                                        dataInBytes[ori_index + i * 4] = (byte)(dataInBytes[ori_index + i * 4] * alpha);
                                        dataInBytes[ori_index + i * 4 + 1] = (byte)(dataInBytes[ori_index + i * 4 + 1] * alpha);
                                        dataInBytes[ori_index + i * 4 + 2] = (byte)(dataInBytes[ori_index + i * 4 + 2] * alpha + 255 * alpha);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public SoftwareBitmap AugmentClassDisplay(SoftwareBitmap ori, TensorFloat flatten)
        {
            int w = ori.PixelWidth;
            int h = ori.PixelHeight;
            var data = flatten.GetAsVectorView();
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

                        // Add a red box to the crack
                        BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                        int startInd = bufferLayout.StartIndex;
                        int block_size = w / 3;
                        for (int i = 0; i < data.Count; i++)
                        {
                            int row = i / 3;
                            int col = i % 3;
                            //int x = startInd
                            if (data.ElementAt(i) > 0)
                            {
                                // Draw four lines as a box
                                int start = startInd + 4 * col * block_size + 4 * row * block_size * h;
                                for (int j = 0; j < block_size; j++)
                                {
                                    dataInBytes[start + j * 4 + 0] = 0;
                                    dataInBytes[start + j * 4 + 1] = 0;
                                    dataInBytes[start + j * 4 + 2] = 255;
                                }
                                for (int j = 0; j < block_size; j++)
                                {
                                    dataInBytes[start + j * 4 * w + 0] = 0;
                                    dataInBytes[start + j * 4 * w + 1] = 0;
                                    dataInBytes[start + j * 4 * w + 2] = 255;
                                }
                                start = startInd + 4 * (col + 1) * block_size + 4 * (row + 1) * block_size * h - 4;
                                for (int j = 0; j < block_size; j++)
                                {
                                    dataInBytes[start - j * 4 + 0] = 0;
                                    dataInBytes[start - j * 4 + 1] = 0;
                                    dataInBytes[start - j * 4 + 2] = 255;
                                }
                                for (int j = 0; j < block_size; j++)
                                {
                                    dataInBytes[start - j * 4 * w + 0] = 0;
                                    dataInBytes[start - j * 4 * w + 1] = 0;
                                    dataInBytes[start - j * 4 * w + 2] = 255;
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }
    }
}
