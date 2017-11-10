using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AutoCruise
{
    public class Parameters : DependencyObject
    {
        public static readonly DependencyProperty PerspectiveAmountProperty =
            DependencyProperty.Register("PerspectiveAmount", typeof(double), typeof(Parameters));
        public double PerspectiveAmount
        {
            get
            {
                return this.Dispatcher.Invoke(() => (double)GetValue(PerspectiveAmountProperty));
            }
            set
            {
                var perspectiveAmount = value;
                if (perspectiveAmount < 0)
                {
                    perspectiveAmount = 0;
                }
                else if (perspectiveAmount > 1)
                {
                    perspectiveAmount = 1;
                }
                SetValue(PerspectiveAmountProperty, perspectiveAmount);
            }
        }

        public static readonly DependencyProperty SobelAvgOutFilterProperty =
            DependencyProperty.Register("SobelAvgOutFilter", typeof(int), typeof(Parameters));
        public int SobelAvgOutFilter
        {
            get
            {
                return this.Dispatcher.Invoke(() => (int)GetValue(SobelAvgOutFilterProperty));
            }
            set
            {
                SetValue(SobelAvgOutFilterProperty, value);
            }
        }

        public static readonly DependencyProperty MinClusterHeightProperty =
            DependencyProperty.Register("MinClusterHeight", typeof(int), typeof(Parameters));
        public int MinClusterHeight
        {
            get
            {
                return this.Dispatcher.Invoke(() => (int)GetValue(MinClusterHeightProperty));
            }
            set
            {
                SetValue(MinClusterHeightProperty, value);
            }
        }

        public static readonly DependencyProperty SteeringProperty =
            DependencyProperty.Register("Steering", typeof(float), typeof(Parameters));
        public float Steering
        {
            get
            {
                return this.Dispatcher.Invoke(() => (float)GetValue(SteeringProperty));
            }
            set
            {
                this.Dispatcher.Invoke(() => SetValue(SteeringProperty, value));
            }
        }

        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(float), typeof(Parameters));
        public float Speed
        {
            get
            {
                return this.Dispatcher.Invoke(() => (float)GetValue(SpeedProperty));
            }
            set
            {
                this.Dispatcher.Invoke(() => SetValue(SpeedProperty, value));
            }
        }
    }
}
