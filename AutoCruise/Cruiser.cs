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
            Parameters = new Parameters()
            {
                PerspectiveAmount = 0.81,
                SobelAvgOutFilter = 10
            };
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
                var perspectiveImg = img.Copy();
                img = img.Convert<Gray, float>().Sobel(1, 0, 1).Convert<Gray, Byte>();
                img = FilterOutSobel(img, perspectiveImg);
                img = FilterPixelClusters(img);
                //img = MarkInsideLanes(img);

                //img = Hough(img);
                _imageViewer.Image = img;
            }
        }

        //private int laneWindowWidth = 40;
        //private int laneWindowHeight = 20;
        //private Image<Gray, byte> MarkInsideLanes(Image<Gray, byte> img)
        //{
        //    Tuple<int, int> laneHorizCenters = EstimateLaneHorizCenters(img);

        //    //sliding window
        //    for (int y = img.Rows - 1; y > 0; y -= laneWindowHeight)
        //    {

        //    }
        //}

        //private Tuple<int, int> EstimateLaneHorizCenters(Image<Gray, byte> img)
        //{
        //    //histogram on lower part of image
        //}

        private int minClusterHeight = 15;
        private Image<Gray, byte> FilterPixelClusters(Image<Gray, byte> img)
        {
            var data = img.Data;
            var width = img.Cols;
            var height = img.Rows;
            var searchedPixels = new bool[width, height];

            for (var y = height - 1; y >= 0; y--)
                for (var x = width - 1; x >= 0; x--)
                {
                    int miny = height; int maxy = 0;
                    RecursiveFindClusterMinMaxY(x, y, width - 1, height - 1, img, ref searchedPixels, ref miny, ref maxy);
                    int clusterHeight = maxy - miny;

                    if (clusterHeight >= 0 && clusterHeight < minClusterHeight)
                    {
                        RecursiveDeleteCluster(x, y, width - 1, height - 1, img);
                    }
                }

            return img;
        }

        private void RecursiveDeleteCluster(int x, int y, int maxXindex, int maxYindex, Image<Gray, byte> img)
        {
            if (x >= 0 && x <= maxXindex && y >= 0 && y <= maxYindex && img.Data[y, x, 0] > 0)
            {
                img.Data[y, x, 0] = 0;

                RecursiveDeleteCluster(x, y - 1, maxXindex, maxYindex, img);
                RecursiveDeleteCluster(x, y + 1, maxXindex, maxYindex, img);
                RecursiveDeleteCluster(x + 1, y - 1, maxXindex, maxYindex, img);
                RecursiveDeleteCluster(x - 1, y - 1, maxXindex, maxYindex, img);
                RecursiveDeleteCluster(x + 1, y + 1, maxXindex, maxYindex, img);
                RecursiveDeleteCluster(x - 1, y + 1, maxXindex, maxYindex, img);
            }
        }

        private void RecursiveFindClusterMinMaxY(int x, int y, int maxXindex, int maxYindex, Image<Gray, byte> img, ref bool[,] searchedPixels, ref int miny, ref int maxy)
        {
            if (x >= 0 && x <= maxXindex && y >= 0 && y <= maxYindex && !searchedPixels[x, y])
            {
                searchedPixels[x, y] = true;

                if (img.Data[y, x, 0] > 0)
                {
                    miny = y < miny ? y : miny;
                    maxy = y > maxy ? y : maxy;

                    RecursiveFindClusterMinMaxY(x, y - 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy);
                    RecursiveFindClusterMinMaxY(x, y + 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy);
                    RecursiveFindClusterMinMaxY(x + 1, y - 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy);
                    RecursiveFindClusterMinMaxY(x - 1, y - 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy);
                    RecursiveFindClusterMinMaxY(x + 1, y + 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy);
                    RecursiveFindClusterMinMaxY(x - 1, y + 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy);
                }
            }
        }

        private Image<Gray, byte> FilterOutSobel(Image<Gray, byte> img, Image<Gray, byte> perspectiveImg)
        {
            var data = img.Data;
            var imgRows = img.Rows;

            //avg
            int sum = 0;
            for (var y = img.Rows - 1; y >= 0; y--)
                for (var x = img.Cols - 1; x >= 0; x--)
                {
                    sum += data[y, x, 0];
                }
            var avg = sum / ((img.Cols - 1) * (img.Rows - 1));

            var perspectiveImgData = perspectiveImg.Data;
            //leave only things differing from average and
            //where perspective image is not black (is in ROI) + sobel image border margin
            int filter = Parameters.SobelAvgOutFilter;
            for (var y = img.Rows - 1; y >= 0; y--)
                for (var x = img.Cols - 1; x >= 0; x--)
                {
                    bool differsFromAvg = (Math.Abs(data[y, x, 0] - avg) > filter);
                    bool isInRoi = (y < (imgRows - 1 - 2) && (perspectiveImgData[y + 2, x, 0] > 0));
                    data[y, x, 0] = differsFromAvg && isInRoi ? data[y, x, 0] : (byte)0;
                }

            return img;
        }

        float[,] GetROI()
        {
            int bottomWidth = (int)(Width * Parameters.PerspectiveAmount);
            int horizontalShrink = 20;
            float[,] destPoints = { { horizontalShrink -(bottomWidth / 2), 0 }, { -horizontalShrink+Width + (bottomWidth / 2), 0 },
                { -horizontalShrink+Width - (bottomWidth / 2), Height }, { horizontalShrink+(bottomWidth / 2), Height } };

            return destPoints;
        }

        private Image<Gray, byte> Perspective(Image<Gray, byte> img)
        {
            int bottomHeightIgnore = img.Height / 6;
            float[,] sourcePoints = { { 0, img.Height / 2 + 5 }, { img.Width, img.Height / 2 + 5 }, { img.Width, img.Height - bottomHeightIgnore }, { 0, img.Height - bottomHeightIgnore } };
            float[,] destPoints = GetROI();

            Emgu.CV.Matrix<float> sourceMat = new Matrix<float>(sourcePoints);
            Emgu.CV.Matrix<float> destMat = new Matrix<float>(destPoints);

            Emgu.CV.Matrix<float> perspMat = new Matrix<float>(3, 3);
            CvInvoke.FindHomography(sourceMat, destMat, perspMat, Emgu.CV.CvEnum.HomographyMethod.Default, 3.0);

            return img.WarpPerspective(perspMat, Emgu.CV.CvEnum.Inter.Linear, Emgu.CV.CvEnum.Warp.FillOutliers, Emgu.CV.CvEnum.BorderType.Constant, new Gray(0));
        }

        private Image<Gray, byte> Hough(Image<Gray, byte> img)
        {
            //img = img.PyrDown().PyrUp();
            img = img.Canny(80, 120);
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
