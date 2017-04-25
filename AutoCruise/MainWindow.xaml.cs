using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace AutoCruise
{
    public partial class MainWindow : Window
    {
        private Cruiser _cruise;

        public MainWindow()
        {
            InitializeComponent();

            _cruise = new Cruiser();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _cruise.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //using (ScreenCapture screenCapture = new ScreenCapture())
            //{
            //    var scrShot = screenCapture.GetScreenShot();
            //    scrShot.Save("D:\\test.jpg");

            //    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(scrShot.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            //    var imgBrush = new ImageBrush();
            //    this.Background = imgBrush;
            //}
            _cruise.StartCruising();
        }
    }
}
