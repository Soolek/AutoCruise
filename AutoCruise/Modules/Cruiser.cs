using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AutoCruise.ScreenCapture;
using Emgu.CV;
using Emgu.CV.Structure;
using AutoCruise.Control;
using AutoCruise.ImageViewer;

namespace AutoCruise.Modules
{
    public class Cruiser : IDisposable
    {
        private CancellationTokenSource _cancelToken;
        private Thread _cruiseThread;

        int Width = 640;
        int Height = 480;

        private bool _startingEngine = false;

        private Parameters _parameters;
        private IScreenCapture _screenCapture;
        private IControl _control;
        private IImageViewer _imageViewer;

        public Cruiser(Parameters parameters, IScreenCapture screenCapture, IControl control, IImageViewer imageViewer)
        {
            _parameters = parameters;
            _screenCapture = screenCapture;
            _control = control;
            _imageViewer = imageViewer;

            _parameters.AutoDrive = false;
            _parameters.PerspectiveAmount = 0.82;
            _parameters.SobelAvgOutFilter = 9;
            _parameters.MinClusterHeight = 10;
            _parameters.Steering = 0;

            _cancelToken = new CancellationTokenSource();
        }

        public void Start()
        {
            DateTime time = DateTime.Now;
            int frameCounter = 0;

            if (_cruiseThread != null)
            {
                return;
            }
            _cruiseThread = new Thread(() =>
                {
                    try
                    {
                        while (!_cancelToken.IsCancellationRequested)
                        {
                            Work();
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
                    { }
                });
            _cruiseThread.Start();
        }



        private float _prevLateral = 0;
        private float _prevLongitudal = 0;
        private void Work()
        {
            using (var screenShot = _screenCapture.GetScreenShot())
            {
                List<System.Drawing.Point> leftPoints, rightPoints;
                int imageStep = 0;

                //IMAGE PROCESSING
                var imgRgb = new Image<Rgb, UInt16>(screenShot);
                imgRgb = MakeGreenToBlack(imgRgb);
                var img = imgRgb.Convert<Gray, Byte>();
                img = img.Resize(Width, Height, Emgu.CV.CvEnum.Inter.Linear);
                ShowSelectedImage(img, imageStep++);

                img = Perspective(img);
                ShowSelectedImage(img, imageStep++);

                img = Blur(img);
                ShowSelectedImage(img, imageStep++);

                var perspectiveImg = img.Copy();
                img = img.Convert<Gray, float>().Sobel(1, 0, 3).Convert<Gray, Byte>();
                ShowSelectedImage(img, imageStep++);

                img = FilterOutSobel(img, perspectiveImg);
                ShowSelectedImage(img, imageStep++);

                //img = FilterPixelClusters(img);
                //ShowSelectedImage(img, imageStep++);

                img = MarkLanes(img, out leftPoints, out rightPoints);
                ShowSelectedImage(img, imageStep++);

                _parameters.MaxImageStep = imageStep - 1;

                //CONTROL
                float steering = 0;

                //steer to center of lane
                int maxYpoints = 5;
                float laneSteering = 0;
                for (int y = 2; y <= maxYpoints; y++)
                {
                    var laneCenterOffset = leftPoints[y].X + rightPoints[y].X - Width;
                    laneSteering += (float)laneCenterOffset * 3 / Width;
                }
                laneSteering /= maxYpoints;
                steering += laneSteering; //* 3f / 2f;

                //steer parallel to lane
                float directionSteering = 0;
                for (int y = 3; y <= 5; y++)
                {
                    directionSteering +=
                        (rightPoints[y + 1].X - rightPoints[y].X) / 40f
                    + (leftPoints[y + 1].X - leftPoints[y].X) / 40f;
                }
                steering += directionSteering * 2f / 3f;

                _parameters.Steering = steering;

                float straightness = Math.Min(LaneStraightness(leftPoints), LaneStraightness(rightPoints));
                float desiredSpeed = 4 + 10f * (straightness * straightness);
                var longitudal = Math.Min(1, Math.Max(-1, (desiredSpeed - _parameters.Speed) / 8));
                _parameters.Acc = Math.Max(0, longitudal);
                _parameters.Brake = Math.Max(0, -longitudal);

                if (_parameters.AutoDrive)
                {
                    var lateralDamper = Math.Max(0.1f, (Math.Abs(steering) - Math.Abs(_prevLateral)) * 10f);
                    var dampedSteering = (lateralDamper * _prevLateral + steering) / (lateralDamper + 1f);
                    _prevLateral = dampedSteering;//steering;
                    _control.SetLateral(dampedSteering);

                    _prevLongitudal = (_prevLongitudal * 2 + longitudal) / 3f;
                    _control.SetLongitudal(_prevLongitudal);

                    //brake if going backwards
                    if (_parameters.Gear == 0 && _parameters.Speed > 0.5)
                    {
                        _control.SetLongitudal(-1);
                        _control.SetLateral(0);
                    }
                    //set Drive if reverse and standing still
                    else if (_parameters.Gear <= 1 && _parameters.Speed < 1) 
                    {
                        _control.ShiftUp();
                    }
                    //start engine
                    if (_parameters.Rpm < 300 && !_startingEngine)
                    {
                        _control.Ignition();
                        _startingEngine = true;
                    }
                    if (_parameters.Rpm > 400 && _startingEngine)
                    {
                        _startingEngine = false;
                    }
                }
                else
                {
                    //_control.Reset();
                }
            }
        }

        private void ShowSelectedImage(Image<Gray, byte> img, int imageStep)
        {
            if (imageStep == _parameters.ImageStep)
            {
                _imageViewer.SetImage(img.Clone());
            }
        }

        private float maxSumOfDifferences = 50;
        private float LaneStraightness(List<System.Drawing.Point> lanePoints)
        {
            var xsToCompare = lanePoints
                            .Skip(2)
                            //.Take(12)
                            .Select(p => p.X)
                            .ToArray();

            int sumOfDifferences = 0;
            for (int i = 1; i < xsToCompare.Length; i++)
            {
                var diff = Math.Abs(xsToCompare[i] - xsToCompare[i - 1]);
                diff *= diff;
                sumOfDifferences += diff;
            }

            maxSumOfDifferences = sumOfDifferences * 2 / 3 > maxSumOfDifferences ? sumOfDifferences * 2 / 3 : maxSumOfDifferences;

            float laneCurvative = Math.Min(1f, sumOfDifferences / maxSumOfDifferences);
            return 1f - laneCurvative;
        }

        private int laneWindowWidth = 40;
        private int laneWindowHeight = 20;
        private Image<Gray, byte> MarkLanes(Image<Gray, byte> img, out List<System.Drawing.Point> leftPoints, out List<System.Drawing.Point> rightPoints)
        {
            leftPoints = new List<System.Drawing.Point>();
            rightPoints = new List<System.Drawing.Point>();
            Tuple<int, int> laneHorizCenters = EstimateLaneHorizCenters(img);
            var data = img.Data;

            //sliding window
            int windowMeanLeftX = laneHorizCenters.Item1;
            windowMeanLeftX = windowMeanLeftX < laneWindowWidth / 2 ? laneWindowWidth / 2 :
                    windowMeanLeftX > (Width - laneWindowWidth / 2) ? (Width - laneWindowWidth / 2) :
                    windowMeanLeftX;
            int windowMeanRightX = laneHorizCenters.Item2;
            windowMeanRightX = windowMeanRightX < laneWindowWidth / 2 ? laneWindowWidth / 2 :
                    windowMeanRightX > (Width - laneWindowWidth / 2) ? (Width - laneWindowWidth / 2) :
                    windowMeanRightX;
            for (int y = img.Rows - 1; y > laneWindowHeight; y -= laneWindowHeight)
            {
                //find mean of a left window
                var leftX = FindWindowMeanX(img,
                    new System.Drawing.Point(windowMeanLeftX - laneWindowWidth / 2, y - laneWindowHeight),
                    laneWindowWidth, laneWindowHeight);
                if (leftX == -1)
                {
                    windowMeanLeftX = UsePreviousPoints(windowMeanLeftX, leftPoints);
                }
                else
                {
                    windowMeanLeftX = leftX;
                }

                windowMeanLeftX = windowMeanLeftX < laneWindowWidth / 2 ? laneWindowWidth / 2 :
                    windowMeanLeftX > (Width - laneWindowWidth / 2) ? (Width - laneWindowWidth / 2) :
                    windowMeanLeftX;

                //find mean of a right window
                var rightX = FindWindowMeanX(img,
                    new System.Drawing.Point(windowMeanRightX - laneWindowWidth / 2, y - laneWindowHeight),
                    laneWindowWidth, laneWindowHeight);
                if (rightX == -1)
                {
                    windowMeanRightX = UsePreviousPoints(windowMeanRightX, rightPoints);
                }
                else
                {
                    windowMeanRightX = rightX;
                }

                windowMeanRightX = windowMeanRightX < laneWindowWidth / 2 ? laneWindowWidth / 2 :
                    windowMeanRightX > (Width - laneWindowWidth / 2) ? (Width - laneWindowWidth / 2) :
                    windowMeanRightX;

                for (int x = -laneWindowWidth / 2; x < laneWindowWidth / 2; x++)
                {
                    data[y - laneWindowHeight / 2, windowMeanLeftX + x, 0] = 255;
                    data[y - laneWindowHeight / 2, windowMeanRightX + x, 0] = 255;
                }

                leftPoints.Add(new System.Drawing.Point(windowMeanLeftX, y));
                rightPoints.Add(new System.Drawing.Point(windowMeanRightX, y));
            }

            return img;
        }

        private int UsePreviousPoints(int foundX, List<System.Drawing.Point> prevPoints)
        {
            if (prevPoints.Count < 2)
            {
                return foundX;
            }

            return 2 * prevPoints[prevPoints.Count - 1].X - prevPoints[prevPoints.Count - 2].X;
        }

        private int FindWindowMeanX(Image<Gray, byte> img, System.Drawing.Point topLeft, int windowWidth, int windowHeight)
        {
            var data = img.Data;
            long weightedIndexSum = 0;
            long weightedSum = 0;

            for (int y = 0; y < windowHeight; y++)
                for (int x = 0; x < windowWidth; x++)
                {
                    var val = data[topLeft.Y + y, topLeft.X + x, 0] > 128 ? data[topLeft.Y + y, topLeft.X + x, 0] - 128 : 0;
                    val *= val;
                    weightedIndexSum += x * val;
                    weightedSum += val;
                }

            if (weightedSum == 0)
                //return topLeft.X + windowWidth / 2;
                return -1;

            return topLeft.X + (int)Math.Round(weightedIndexSum * 1.0 / weightedSum);
        }

        private Tuple<int, int> EstimateLaneHorizCenters(Image<Gray, byte> img)
        {
            var data = img.Data;
            var width = img.Cols;
            var height = img.Rows;

            //left lane
            int[] sums = new int[width];
            for (var y = height - 30; y > 0 && (y >= (height * 2 / 3) || sums.Sum() <= 0); y--)
                for (var x = 0; x < width / 2; x++)
                {
                    var val = data[y, x, 0];

                    if (val > 0)
                    {
                        sums[x] += val;
                    }
                }

            var maxLeftIndex = width / 4;
            for (var x = 0; x < width / 2; x++)
            {
                if (sums[x] > sums[maxLeftIndex])
                {
                    maxLeftIndex = x;
                }
            }

            //right lane
            sums = new int[width];
            for (var y = height - 30; y > 0 && (y >= (height * 2 / 3) || sums.Sum() <= 0); y--)
                for (var x = width / 2; x < width; x++)
                {
                    var val = data[y, x, 0];

                    if (val > 0)
                    {
                        sums[x] += val;
                    }
                }

            var maxRightIndex = width * 3 / 4;
            for (var x = width / 2; x < width; x++)
            {
                if (sums[x] > sums[maxRightIndex])
                {
                    maxRightIndex = x;
                }
            }

            return new Tuple<int, int>(maxLeftIndex, maxRightIndex);
        }

        private Image<Gray, byte> FilterPixelClusters(Image<Gray, byte> img)
        {
            int minClusterHeight = _parameters.MinClusterHeight;

            var data = img.Data;
            var width = img.Cols;
            var height = img.Rows;
            var searchedPixels = new bool[width, height];

            for (var y = height - 1; y >= 0; y--)
                for (var x = width - 1; x >= 0; x--)
                {
                    int miny = height; int maxy = 0;
                    RecursiveFindClusterMinMaxY(x, y, width - 2, height - 2, img, ref searchedPixels, ref miny, ref maxy);
                    int clusterHeight = maxy - miny;

                    if (clusterHeight >= 0 && clusterHeight < minClusterHeight)
                    {
                        RecursiveDeleteCluster(x, y, width - 1, height - 1, img);
                    }
                }

            return img;
        }

        private void RecursiveDeleteCluster(int x, int y, int maxXindex, int maxYindex, Image<Gray, byte> img, int recur = 0)
        {
            if (recur++ > 3000)
                return;

            if (x >= 0 && x <= maxXindex && y >= 0 && y <= maxYindex && img.Data[y, x, 0] > 0)
            {
                img.Data[y, x, 0] = 0;

                RecursiveDeleteCluster(x, y - 1, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x + 1, y - 1, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x - 1, y - 1, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x + 1, y, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x - 1, y, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x + 1, y + 1, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x - 1, y + 1, maxXindex, maxYindex, img, recur);
                RecursiveDeleteCluster(x, y + 1, maxXindex, maxYindex, img, recur);
            }
        }

        private void RecursiveFindClusterMinMaxY(int x, int y, int maxXindex, int maxYindex, Image<Gray, byte> img, ref bool[,] searchedPixels, ref int miny, ref int maxy, int recur = 0)
        {
            if (recur++ > 3000)
                return;

            if (x >= 0 && x <= maxXindex && y >= 0 && y <= maxYindex && !searchedPixels[x, y])
            {
                searchedPixels[x, y] = true;

                if (img.Data[y, x, 0] > 0)
                {
                    miny = y < miny ? y : miny;
                    maxy = y > maxy ? y : maxy;

                    RecursiveFindClusterMinMaxY(x, y - 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x + 1, y - 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x - 1, y - 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x + 1, y, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x - 1, y, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x + 1, y + 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x - 1, y + 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
                    RecursiveFindClusterMinMaxY(x, y + 1, maxXindex, maxYindex, img, ref searchedPixels, ref miny, ref maxy, recur);
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
            int filter = _parameters.SobelAvgOutFilter;
            for (var y = img.Rows - 1; y >= 0; y--)
                for (var x = img.Cols - 1; x >= 0; x--)
                {
                    var differenceFromAvg = Math.Abs(data[y, x, 0] - avg);
                    bool isInRoi = (y < (imgRows - 1 - 2) && (perspectiveImgData[y + 2, x, 0] > 0));
                    data[y, x, 0] = differenceFromAvg > filter && isInRoi ? (byte)(differenceFromAvg + 128) : (byte)0;
                }

            return img;
        }

        private Image<Gray, byte> Blur(Image<Gray, byte> img)
        {
            return img.SmoothBlur(4, 6);
        }

        float[,] GetROI()
        {
            int bottomWidth = (int)(Width * _parameters.PerspectiveAmount);
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

        private Image<Rgb, ushort> MakeGreenToBlack(Image<Rgb, ushort> imgRgb)
        {
            var data = imgRgb.Data;
            var rows = imgRgb.Rows;
            var cols = imgRgb.Cols;

            for (int y = rows - 1; y >= 0; y--)
                for (int x = cols - 1; x >= 0; x--)
                {
                    var greenish = (data[y, x, 1] - data[y, x, 0]) + (data[y, x, 1] - data[y, x, 2]);
                    if (greenish > 0)
                    {
                        var green = (int)data[y, x, 1] - greenish;
                        data[y, x, 1] = green < 0 ? (ushort)0 :
                            green > byte.MaxValue ? (ushort)byte.MaxValue :
                            (ushort)green;

                        double invGreenIntensity = 1.0 - ((double)greenish / (2 * byte.MaxValue));
                        data[y, x, 0] = (byte)(data[y, x, 0] * invGreenIntensity * invGreenIntensity);
                        data[y, x, 2] = (byte)(data[y, x, 2] * invGreenIntensity * invGreenIntensity);
                    }
                }

            return imgRgb;
        }

        public void Dispose()
        {
            _cancelToken.Cancel();
            Thread.Sleep(500);
            if (_cruiseThread != null)
            {
                _cruiseThread.Abort();
            }
        }
    }
}
