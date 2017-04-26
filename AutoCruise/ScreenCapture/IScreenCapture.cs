using System;
using System.Drawing;

namespace AutoCruise.ScreenCapture
{
    public interface IScreenCapture : IDisposable
    {
        Bitmap GetScreenShot();
    }
}