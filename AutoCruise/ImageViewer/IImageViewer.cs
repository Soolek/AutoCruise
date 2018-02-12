using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCruise.ImageViewer
{
    public interface IImageViewer
    {
        void SetImage(Emgu.CV.IImage image);
    }
}
