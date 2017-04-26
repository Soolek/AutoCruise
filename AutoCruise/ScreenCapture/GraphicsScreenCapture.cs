using AutoCruise.ScreenCapture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AutoCruise.ScreenCapture
{
    public class GraphicsScreenCapture : IScreenCapture
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

        public Bitmap GetScreenShot()
        {
            var bounds = GetBounds();

            Bitmap bmpScreenCapture = new Bitmap((int)bounds.Width, (int)bounds.Height);

            using (Graphics g = Graphics.FromImage(bmpScreenCapture))
            {
                g.CopyFromScreen((int)bounds.Left, (int)bounds.Top, 0, 0,
                                 bmpScreenCapture.Size,
                                 CopyPixelOperation.SourceCopy);
            }

            return bmpScreenCapture;
        }

        IntPtr _gamehWnd = IntPtr.Zero;
        private Rectangle GetBounds()
        {
            if (_gamehWnd == IntPtr.Zero)
            {
                _gamehWnd = FindWindow(null, "Live for Speed");
            }

            if (_gamehWnd != IntPtr.Zero)
            {
                RECT rct = new RECT();
                if (GetWindowRect(_gamehWnd, out rct))
                {
                    return new Rectangle((int)rct.Left+10, (int)rct.Top+30, (int)rct.Right - (int)rct.Left -20, (int)rct.Bottom - (int)rct.Top -30);
                }
            }

            return new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        }

        public void Dispose()
        {
        }
    }
}
