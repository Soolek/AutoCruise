using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Drawing;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace AutoCruise.ScreenCapture
{
    public class SharpDXScreenCapture : IScreenCapture
    {
        private Device _device;
        private Texture2DDescription _textureDesc;
        private OutputDuplication _duplicatedOutput;
        private Texture2D _screenTexture;

        private int _width;
        private int _height;

        public SharpDXScreenCapture()
        {
            // # of graphics card adapter
            const int numAdapter = 0;

            // # of output device (i.e. monitor)
            const int numOutput = 0;

            // Create DXGI Factory1
            var factory = new Factory1();
            var adapter = factory.GetAdapter1(numAdapter);

            // Create device from Adapter
            _device = new Device(adapter);

            // Get DXGI.Output
            var output = adapter.GetOutput(numOutput);
            var output1 = output.QueryInterface<Output1>();

            // Width/Height of desktop to capture
            _width = output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left;
            _height = output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top;

            // Create Staging texture CPU-accessible
            _textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _width,
                Height = _height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            _screenTexture = new Texture2D(_device, _textureDesc);

            // Duplicate the output
            _duplicatedOutput = output1.DuplicateOutput(_device);
        }

        public Bitmap GetScreenShot()
        {
            var bitmap = new System.Drawing.Bitmap(_width, _height, PixelFormat.Format32bppArgb);

            try
            {
                SharpDX.DXGI.Resource screenResource;
                OutputDuplicateFrameInformation duplicateFrameInformation;

                // Try to get duplicated frame within given time
                _duplicatedOutput.AcquireNextFrame(10000, out duplicateFrameInformation, out screenResource);

                // copy resource into memory that can be accessed by the CPU
                using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                    _device.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);

                // Get the desktop capture texture
                var mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                // Create Drawing.Bitmap

                var boundsRect = new System.Drawing.Rectangle(0, 0, _width, _height);

                // Copy pixels from screen capture Texture to GDI bitmap
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var sourcePtr = mapSource.DataPointer;
                var destPtr = mapDest.Scan0;
                for (int y = 0; y < _height; y++)
                {
                    // Copy a single line 
                    Utilities.CopyMemory(destPtr, sourcePtr, _width * 4);

                    // Advance pointers
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                // Release source and dest locks
                bitmap.UnlockBits(mapDest);
                _device.ImmediateContext.UnmapSubresource(_screenTexture, 0);

                screenResource.Dispose();
                _duplicatedOutput.ReleaseFrame();
            }
            catch (SharpDXException e)
            {
                if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    throw e;
                }
            }

            return bitmap;
        }

        public void Dispose()
        {
            _screenTexture.Dispose();
            _duplicatedOutput.Dispose();
            _device.Dispose();
        }
    }
}
