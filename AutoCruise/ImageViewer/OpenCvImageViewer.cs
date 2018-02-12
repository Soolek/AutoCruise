using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.UI;

namespace AutoCruise.ImageViewer
{
    public class OpenCvImageViewer : IImageViewer, IDisposable
    {
        private Emgu.CV.UI.ImageViewer _imageViewer;

        public OpenCvImageViewer()
        {
            _imageViewer = new Emgu.CV.UI.ImageViewer() { Width = 640, Height = 480 };
            _imageViewer.Show();
        }

        public void Dispose()
        {
            _imageViewer.Dispose();
            _imageViewer = null;
        }

        public void SetImage(Emgu.CV.IImage image)
        {
            if (_imageViewer != null)
            {
                _imageViewer.Image = image;
            }
        }
    }
}
