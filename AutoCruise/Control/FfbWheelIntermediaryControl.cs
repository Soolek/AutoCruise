using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace AutoCruise.Control
{
    public class FfbWheelIntermediaryControl : IControl
    {
        public IControl OutputControl { get; private set; }

        public FfbWheelIntermediaryControl(IControl outputControl)
        {
            OutputControl = outputControl;
        }

        private bool _logiInitialized = false;
        public void SetLateral(float lateral)
        {
            if (!_logiInitialized)
            {
                LogitechGSDK.LogiSteeringInitialize(true);
                _logiInitialized = true;
            }


            if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
            {
                if (lateral == 0)
                {
                    LogitechGSDK.LogiStopSpringForce(0);
                }
                else
                {
                    LogitechGSDK.LogiPlaySpringForce(0, (int)(lateral * 90), 90, 90);
                }
            }

            OutputControl.SetLateral(lateral);
        }

        public void SetLongitudal(float longitudal)
        {
            OutputControl.SetLongitudal(longitudal);
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
            LogitechGSDK.LogiSteeringShutdown();
        }
    }
}
