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
    }
}
