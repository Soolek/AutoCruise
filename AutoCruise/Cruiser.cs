using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using AutoCruise.Main;
using AutoCruise.ScreenCapture;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace AutoCruise
{
    public class Cruiser : IDisposable
    {
        private CancellationTokenSource _cancelToken;
        private Thread _cruiseThread;
        private ImageViewer _imageViewer;

        int width = 640;
        int height = 480;

        public Cruiser()
        {
            _cancelToken = new CancellationTokenSource();
            _imageViewer = new ImageViewer() { Width = width, Height = height };
        }

        public void StartCruising()
        {
            _imageViewer.Show();
            DateTime time = DateTime.Now;
            int frameCounter = 0;

            if (_cruiseThread != null)
            {
                return;
            }
            _cruiseThread = new Thread(() =>
                {
                    var screenCapture = new GraphicsScreenCapture();
                    try
                    {
                        while (!_cancelToken.IsCancellationRequested)
                        {
                            Work(screenCapture);
                            frameCounter++;
                            if ((DateTime.Now - time).TotalSeconds > 1)
                            {
                                Debug.WriteLine("FPS: " + frameCounter);
                                frameCounter = 0;
                                time = DateTime.Now;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    finally
                    {
                        screenCapture.Dispose();
                    }
                });
            _cruiseThread.Start();
        }

        private void Work(IScreenCapture screenCapture)
        {
            using (var screenShot = screenCapture.GetScreenShot())
            {
                var img = new Image<Rgb, UInt16>(screenShot).Convert<Gray, Byte>();
                img = ResizeCrop(img);
                img = img.Copy(new Rectangle(0, 160, width, height - 160 - 120));
                //img = img.PyrDown().PyrUp();
                //img = Hough(img);

                _imageViewer.Image = img;
            }
        }

        private Image<Gray, byte> Hough(Image<Gray, byte> img)
        {
            return img.Canny(50, 120);
            //var houghLines = img.HoughLines(40, 60, 4, Math.PI / 180, 3, 30, 3);
            //foreach(var houghLine in houghLines)
            //{
            //    foreach(var segment in houghLine)
            //    {
            //        img.Draw(segment, new Gray(255), 3);
            //    }
            //}
            return img;
        }

        private Image<Gray, Byte> ResizeCrop(Image<Gray, Byte> img)
        {
            int ratio = img.Width * 100 / img.Height;
            int newWidth = width;
            int newHeight = height;

            if (ratio > (width * 100 / height))
            {
                newWidth = height * ratio / 100;
            }
            else
            {
                newHeight = width * 100 / ratio;
            }

            img = img.Resize(newWidth, newHeight, Emgu.CV.CvEnum.Inter.Linear);

            return img.Copy(new Rectangle((newWidth - width) / 2, (newHeight - height) / 2, width, height));
        }

        public void Dispose()
        {
            _cancelToken.Cancel();
            Thread.Sleep(500);
            if (_cruiseThread != null)
            {
                _cruiseThread.Abort();
            }
            _imageViewer.Dispose();
        }
    }
}
