using AutoCruise.Control;
using AutoCruise.ImageViewer;
using AutoCruise.ScreenCapture;
using Autofac;
using InSimDotNet.Out;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AutoCruise.Modules
{
    public class AutofacConfig
    {
        public static IContainer Container { get; private set; }

        public static void BuildContainer()
        {
            if (Container == null)
            {
                var builder = new ContainerBuilder();

                var parameters = new Parameters();
                builder.RegisterInstance(parameters).As<Parameters>();
                builder.RegisterInstance(GetControl(parameters)).As<IControl>();
                builder.RegisterInstance(GetScreenCapture()).As<IScreenCapture>();
                builder.RegisterInstance(GetOutGauge()).As<OutGauge>();
                builder.RegisterType<OutGaugeParametersUpdater>().As<OutGaugeParametersUpdater>();
                builder.RegisterType<Cruiser>().As<Cruiser>();

                builder.RegisterInstance(new OpenCvImageViewer()).As<IImageViewer>();
                //builder.RegisterType<WpfImageViewer>().As<IImageViewer>();

                Container = builder.Build();
            }
        }

        private static OutGauge GetOutGauge()
        {
            OutGauge outGauge = new OutGauge();
            outGauge.Connect("127.0.0.1", 666);
            return outGauge;
        }

        private static IScreenCapture GetScreenCapture()
        {
            IScreenCapture screenCapture;
            try
            {
                screenCapture = new SharpDxScreenCapture();
                using (var temp = screenCapture.GetScreenShot())
                { }
            }
            catch (Exception ex)
            {
                //Does not work in 8bpp or non-DWM desktop modes
                screenCapture = new GraphicsScreenCapture();
            }
            return screenCapture;
        }

        private static IControl GetControl(Parameters parameters)
        {
            IControl outputControl;
            try
            {
                outputControl = new VJoyControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show("VJoy error, switching to keyboard control as output:" + ex.Message);
                outputControl = new KeyPressEmulator();
            }

            try
            {
                return new FfbWheelIntermediaryControl(outputControl, parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Logitech SDK failed, using pass-through control: "+ex.Message);
                return outputControl;
            }
        }
    }
}
