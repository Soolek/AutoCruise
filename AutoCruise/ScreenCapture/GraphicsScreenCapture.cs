using AutoCruise.ScreenCapture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;

namespace AutoCruise.ScreenCapture
{
    public class GraphicsScreenCapture : IScreenCapture
    {
        private int _width;
        private int _height;

        public GraphicsScreenCapture()
        {
            _width = (int)SystemParameters.PrimaryScreenWidth;
            _height = (int)SystemParameters.PrimaryScreenHeight;
        }

        public Bitmap GetScreenShot()
        {
            Bitmap bmpScreenCapture = new Bitmap(_width, _height);

            using (Graphics g = Graphics.FromImage(bmpScreenCapture))
            {
                g.CopyFromScreen(_width, _height,
                                 0, 0,
                                 bmpScreenCapture.Size,
                                 CopyPixelOperation.SourceCopy);
            }

            return bmpScreenCapture;
        }

        public void Dispose()
        {
        }
    }
}
