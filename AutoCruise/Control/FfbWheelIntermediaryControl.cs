using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using vJoyInterfaceWrap;

namespace AutoCruise.Control
{
    public class FfbWheelIntermediaryControl : IControl
    {
        public IControl OutputControl { get; private set; }

        public FfbWheelIntermediaryControl(IControl outputControl)
        {
            OutputControl = outputControl;
        }

        public void SetLongitudal(float longitudal)
        {
            OutputControl.SetLongitudal(longitudal);
        }

        public void SetLateral(float lateral)
        {
            OutputControl.SetLateral(lateral);
        }

        public void ShiftUp()
        {
            OutputControl.ShiftUp();
        }

        public void ShiftDown()
        {
            OutputControl.ShiftDown();
        }

        public void Ignition()
        {
            OutputControl.Ignition();
        }

        public void Reset()
        {
            OutputControl.Reset();
        }

        public void Dispose()
        {
            OutputControl.Dispose();
        }
    }
}
