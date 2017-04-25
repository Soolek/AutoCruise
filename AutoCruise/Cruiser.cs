using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using AutoCruise.Main;

namespace AutoCruise
{
    public class Cruiser : IDisposable
    {
        private CancellationTokenSource _cancelToken;
        private Thread _cruiseThread;

        public Cruiser()
        {
            _cancelToken = new CancellationTokenSource();
        }

        public void StartCruising()
        {
            DateTime time = DateTime.Now;
            int frameCounter = 0;

            if (_cruiseThread != null)
            {
                return;
            }
            _cruiseThread = new Thread(() =>
                {
                    ScreenCapture screenCapture = new ScreenCapture();
                    try
                    {
                        while (!_cancelToken.IsCancellationRequested)
                        {
                            Work(screenCapture);
                            frameCounter++;
                            if ((DateTime.Now - time).TotalSeconds > 1)
                            {
                                Console.WriteLine("FPS: " + frameCounter);
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

        private void Work(ScreenCapture screenCapture)
        {
            var screenShot = screenCapture.GetScreenShot();
        }

        public void Dispose()
        {
            _cancelToken.Cancel();
        }
    }
}
