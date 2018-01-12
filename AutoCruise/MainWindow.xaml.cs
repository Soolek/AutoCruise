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

            InitializeComponent();
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                Cruiser.Parameters.AutoDrive = !Cruiser.Parameters.AutoDrive;
            }
            else if (e.Key == Key.Z)
            {
                Cruiser.Parameters.ImageStep--;
            }
            else if (e.Key == Key.X)
            {
                Cruiser.Parameters.ImageStep++;
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
