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
                SteeringForGui = new Thickness((value + 1f) * 40f - 3f, 0, 0, 0);
            }
        }

        public static readonly DependencyProperty SteeringForGuiProperty =
            DependencyProperty.Register("SteeringForGui", typeof(Thickness), typeof(Parameters));
        public Thickness SteeringForGui
        {
            get
            {
                return this.Dispatcher.Invoke(() => (Thickness)GetValue(SteeringForGuiProperty));
            }
            set
            {
                this.Dispatcher.Invoke(() => SetValue(SteeringForGuiProperty, value));
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
                SpeedForGui = value;
            }
        }

        private static float _maxSpeed = 0;
        public static readonly DependencyProperty SpeedForGuiProperty =
            DependencyProperty.Register("SpeedForGui", typeof(float), typeof(Parameters));
        public float SpeedForGui
        {
            get
            {
                return this.Dispatcher.Invoke(() => (float)GetValue(SpeedForGuiProperty));
            }
            set
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (value > _maxSpeed)
                    {
                        _maxSpeed = value;
                    }
                    SetValue(SpeedForGuiProperty, value/_maxSpeed*80f);
                });
            }
        }

        public static readonly DependencyProperty AutoDriveProperty =
            DependencyProperty.Register("AutoDrive", typeof(bool), typeof(Parameters));
        public bool AutoDrive
        {
            get
            {
                return this.Dispatcher.Invoke(() => (bool)GetValue(AutoDriveProperty));
            }
            set
            {
                this.Dispatcher.Invoke(() => SetValue(AutoDriveProperty, value));
            }
        }

        public static readonly DependencyProperty ImageStepProperty =
            DependencyProperty.Register("ImageStep", typeof(int), typeof(Parameters));
        public int ImageStep
        {
            get
            {
                return this.Dispatcher.Invoke(() => (int)GetValue(ImageStepProperty));
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                SetValue(ImageStepProperty, value);
            }
        }
    }
}
