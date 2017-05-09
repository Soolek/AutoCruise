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

        int Width = 640;
        int Height = 480;

        public Parameters Parameters { get; private set; }

        public Cruiser()
        {
            Parameters = new Parameters() { PerspectiveAmount = 0.86 };
            _cancelToken = new CancellationTokenSource();
            _imageViewer = new ImageViewer() { Width = Width, Height = Height };
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

                img = Perspective(img);

                var x = img.Convert<Gray, float>().Sobel(1, 0, 3);
                //img = Hough(img);

                _imageViewer.Image = x;
            }
        }

        private Image<Gray, byte> Perspective(Image<Gray, byte> img)
        {
            int bottomWidth = (int)(Width * Parameters.PerspectiveAmount);
            int bottomHeightIgnore = img.Height / 6;
            float[,] sourcePoints = { { 0, img.Height / 2 + 10 }, { img.Width, img.Height / 2 + 10 }, { img.Width, img.Height - bottomHeightIgnore }, { 0, img.Height - bottomHeightIgnore } };
            float[,] destPoints = { { -(bottomWidth / 2), 0 }, { Width + (bottomWidth / 2), 0 }, { Width - (bottomWidth / 2), Height }, { (bottomWidth / 2), Height } };
            Emgu.CV.Matrix<float> sourceMat = new Matrix<float>(sourcePoints);
            Emgu.CV.Matrix<float> destMat = new Matrix<float>(destPoints);

            Emgu.CV.Matrix<float> perspMat = new Matrix<float>(3, 3);
            CvInvoke.FindHomography(sourceMat, destMat, perspMat, Emgu.CV.CvEnum.HomographyMethod.Default, 3.0);

            return img.WarpPerspective(perspMat, Emgu.CV.CvEnum.Inter.Linear, Emgu.CV.CvEnum.Warp.FillOutliers, Emgu.CV.CvEnum.BorderType.Constant, new Gray(0));
        }

        private Image<Gray, byte> Hough(Image<Gray, byte> img)
        {
            //img = img.PyrDown().PyrUp();
            img =  img.Canny(80, 120);
            var houghLines = img.HoughLines(40, 60, 4, Math.PI / 180, 3, 30, 3);
            foreach (var houghLine in houghLines)
            {
                foreach (var segment in houghLine)
                {
                    img.Draw(segment, new Gray(255), 3);
                }
            }
            return img;
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
