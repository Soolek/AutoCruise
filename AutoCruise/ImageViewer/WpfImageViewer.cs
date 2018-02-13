using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace AutoCruise.ImageViewer
{
    public class WpfImageViewer : IImageViewer
    {
        private Parameters _parameters;

        public WpfImageViewer(Parameters parameters)
        {
            _parameters = parameters;
        }

        public void SetImage(IImage image)
        {
            _parameters.Image = image.Bitmap;
        }
    }
}
