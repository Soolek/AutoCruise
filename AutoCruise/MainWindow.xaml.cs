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
        public Cruiser Cruiser { get; private set; }

        public MainWindow()
        {
            Cruiser = new Cruiser();
            this.DataContext = Cruiser;

            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cruiser.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Cruiser.StartCruising();
        }
    }
}
