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
using AutoCruise.Helpers;
using AutoCruise.Control;
using Autofac;
using AutoCruise.Modules;

namespace AutoCruise
{
    public partial class MainWindow : Window
    {
        private Parameters _parameters;
        private IControl _control;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //IoC initialization here due to some of the libraries using pointer to main app window
            AutofacConfig.BuildContainer();

            _control = AutofacConfig.Container.Resolve<IControl>();
            _parameters = AutofacConfig.Container.Resolve<Parameters>();
            this.DataContext = _parameters;

            this.KeyUp += MainWindow_KeyUp;
            this.KeyDown += MainWindow_KeyDown;

            AutofacConfig.Container.Resolve<OutGaugeParametersUpdater>().StartUpdatingParameters();
            AutofacConfig.Container.Resolve<Cruiser>().Start();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.J)
            {
                _control.SetLateral(-1);
            }
            else if (e.Key == Key.L)
            {
                _control.SetLateral(1);
            }

            if (e.Key == Key.I)
            {
                _control.SetLongitudal(1);
            }
            else if (e.Key == Key.K)
            {
                _control.SetLongitudal(-1);
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                _parameters.AutoDrive = !_parameters.AutoDrive;
                if (!_parameters.AutoDrive)
                {
                    _control.SetLateral(null);
                    _control.SetLongitudal(null);
                }
            }
            if (e.Key == Key.Z)
            {
                _parameters.ImageStep--;
            }
            if (e.Key == Key.X)
            {
                _parameters.ImageStep++;
            }

            if (e.Key == Key.J || e.Key == Key.L)
            {
                _control.SetLateral(null);
            }

            if (e.Key == Key.I || e.Key == Key.K)
            {
                _control.SetLongitudal(null);
            }

            if (e.Key == Key.Y)
            {
                _control.ShiftUp();
            }
            else if (e.Key == Key.H)
            {
                _control.ShiftDown();
            }

            if (e.Key == Key.U)
            {
                _control.Ignition();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AutofacConfig.Container.Dispose();
        }
    }
}
