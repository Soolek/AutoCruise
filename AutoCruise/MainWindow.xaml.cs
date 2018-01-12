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

namespace AutoCruise
{
    public partial class MainWindow : Window
    {
        public Cruiser Cruiser { get; private set; }

        public MainWindow()
        {
            Cruiser = new Cruiser();
            this.DataContext = Cruiser;

            this.KeyUp += MainWindow_KeyUp;
            this.KeyDown += MainWindow_KeyDown;

            InitializeComponent();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.J)
            {
                Cruiser.Control.SetLateral(-1);
            }
            else if (e.Key == Key.L)
            {
                Cruiser.Control.SetLateral(1);
            }

            if (e.Key == Key.I)
            {
                Cruiser.Control.SetLongitudal(1);
            }
            else if (e.Key == Key.K)
            {
                Cruiser.Control.SetLongitudal(-1);
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                Cruiser.Parameters.AutoDrive = !Cruiser.Parameters.AutoDrive;
            }
            if (e.Key == Key.Z)
            {
                Cruiser.Parameters.ImageStep--;
            }
            if (e.Key == Key.X)
            {
                Cruiser.Parameters.ImageStep++;
            }

            if (e.Key == Key.J || e.Key == Key.L)
            {
                Cruiser.Control.SetLateral(0);
            }

            if (e.Key == Key.I || e.Key == Key.K)
            {
                Cruiser.Control.SetLongitudal(0);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cruiser.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Cruiser.Start();
        }
    }
}
