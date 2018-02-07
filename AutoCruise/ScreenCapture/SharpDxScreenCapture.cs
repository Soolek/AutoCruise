using AutoCruise.ScreenCapture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.IO;

namespace AutoCruise.ScreenCapture
{
    public class SharpDxScreenCapture : IScreenCapture
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private SharpDX.Direct3D11.Device SharpDxDevice { get; set; }
        private OutputDuplication SharpDxDuplicatedOutput { get; set; }
        private Texture2D SharpDxTexture { get; set; }
        private Rectangle DesktopBounds { get; set; }

        public SharpDxScreenCapture()
        {
            InitSharpDXCapture();
        }

        private void InitSharpDXCapture()
        {
            var bounds = GetBounds();

            var factory = new Factory1();

            //Get adapter
            var adapter = factory.GetAdapter(0);
            //Get device from adapter
            SharpDxDevice = new SharpDX.Direct3D11.Device(adapter);
            //Get front buffer of the adapter
            for (int outputId = 0; outputId < adapter.Outputs.Length; outputId++)
            {
                var output = adapter.GetOutput(outputId);
                var output1 = output.QueryInterface<Output1>();

                var rawDesktopBounds = output.Description.DesktopBounds;
                DesktopBounds = new Rectangle(rawDesktopBounds.Left, rawDesktopBounds.Top, rawDesktopBounds.Right - rawDesktopBounds.Left, rawDesktopBounds.Bottom - rawDesktopBounds.Top);

                if (bounds.Left >= rawDesktopBounds.Left && bounds.Right <= rawDesktopBounds.Right
                    && bounds.Top >= rawDesktopBounds.Top && bounds.Bottom <= rawDesktopBounds.Bottom)
                {
                    SharpDxDuplicatedOutput = output1.DuplicateOutput(SharpDxDevice);
                    break;
                }
            }

            // Create Staging texture CPU-accessible
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = DesktopBounds.Width,
                Height = DesktopBounds.Height,
                //Width = bounds.Width,
                //Height = bounds.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            SharpDxTexture = new Texture2D(SharpDxDevice, textureDesc);
        }

        public Bitmap GetScreenShot()
        {
            var bounds = GetBounds();

            SharpDX.DXGI.Resource screenResource;
            OutputDuplicateFrameInformation duplicateFrameInformation;

            // Try to get duplicated frame within given time is ms
            SharpDxDuplicatedOutput.AcquireNextFrame(30, out duplicateFrameInformation, out screenResource);

            // copy resource into memory that can be accessed by the CPU
            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
            {
                SharpDxDevice.ImmediateContext.CopyResource(screenTexture2D, SharpDxTexture);
                //SharpDxDevice.ImmediateContext.CopySubresourceRegion(screenTexture2D, 0, null, SharpDxTexture, 0);
            }

            // Get the desktop capture texture
            var mapSource = SharpDxDevice.ImmediateContext.MapSubresource(SharpDxTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            // Copy pixels from screen capture Texture to GDI bitmap
            var mapDest = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;

            // Set sourcePtr at window topleft corner
            sourcePtr = IntPtr.Add(sourcePtr, (bounds.Top - DesktopBounds.Top) * mapSource.RowPitch + (bounds.Left - DesktopBounds.Left) * 4);

            for (int y = 0; y < bitmap.Height; y++)
            {
                // Copy a single line 
                Utilities.CopyMemory(destPtr, sourcePtr, bitmap.Width * 4);

                // Advance pointers
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            // Release source and dest locks
            bitmap.UnlockBits(mapDest);
            SharpDxDevice.ImmediateContext.UnmapSubresource(SharpDxTexture, 0);

            screenResource.Dispose();
            SharpDxDuplicatedOutput.ReleaseFrame();

            return bitmap;
        }

        DateTime _lastBoundsCheck = DateTime.MinValue;
        Rectangle? _lastBounds = null;
        IntPtr _gamehWnd = IntPtr.Zero;
        private Rectangle GetBounds()
        {
            if (_lastBounds != null && (DateTime.Now - _lastBoundsCheck).TotalSeconds < 3)
            {
                return _lastBounds.Value;
            }

            if (_gamehWnd == IntPtr.Zero)
            {
                _gamehWnd = FindWindow(null, "Live for Speed");
            }

            if (_gamehWnd != IntPtr.Zero)
            {
                RECT rct = new RECT();
                if (GetWindowRect(_gamehWnd, out rct))
                {
                    _lastBounds = new Rectangle((int)rct.Left + 10, (int)rct.Top + 30, (int)rct.Right - (int)rct.Left - 20, (int)rct.Bottom - (int)rct.Top - 30);
                    return _lastBounds.Value;
                }
            }

            return _lastBounds ?? new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        }

        public void Dispose()
        {
            SharpDxDuplicatedOutput.Dispose();
            SharpDxTexture.Dispose();
        }
    }
}
