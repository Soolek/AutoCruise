using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace AutoCruise.Control
{
    public class FfbWheelIntermediaryControl : IControl
    {
        public IControl OutputControl { get; private set; }

        private int _maxOffset = 80;
        private bool _logiTaskRun = true;
        private Task _logiTask;

        public FfbWheelIntermediaryControl(IControl outputControl)
        {
            OutputControl = outputControl;

            _logiTask = new Task(() =>
            {
                Thread.Sleep(2000);
                while (_logiTaskRun)
                {
                    if (_logiInitialized && LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
                    {
                        var state = LogitechGSDK.LogiGetStateCSharp(0);
                        DoWheelControl(state);
                    }
                    Thread.Sleep(1);
                }
            });
            _logiTask.Start();
        }

        private void DoWheelControl(LogitechGSDK.DIJOYSTATE2ENGINES state)
        {
            float lateral = ((float)state.lX / Int16.MaxValue) / (_maxOffset / 100f);
            OutputControl.SetLateral(lateral);

            if (!Cruiser.Parameters.AutoDrive)
            {
                var pressedButtons = GetPressedButtons(state.rgbButtons);
                var justPressedButtons = GetJustPressedButtons(pressedButtons).ToList();

                float acc = (float)(-state.lY + Int16.MaxValue) / (2 * Int16.MaxValue);
                float brake = (float)(-state.lRz + Int16.MaxValue) / (2 * Int16.MaxValue);
                OutputControl.SetLongitudal(acc - brake);

                if (justPressedButtons.Contains(10))
                {
                    OutputControl.ShiftDown();
                }
                if (justPressedButtons.Contains(11))
                {
                    OutputControl.ShiftUp();
                }
            }
        }

        private List<byte> _prevPressedButtons;
        private byte[] GetJustPressedButtons(List<byte> pressedButtons)
        {
            if (_prevPressedButtons == null)
            {
                _prevPressedButtons = pressedButtons;
                return new byte[0];
            }

            var justPressedButtons = Enumerable.Range(0, 128)
                    .Where(butId => pressedButtons.Contains((byte)butId) && !_prevPressedButtons.Contains((byte)butId))
                    .Select(i => (byte)i)
                    .ToArray();

            _prevPressedButtons = pressedButtons;

            return justPressedButtons;
        }

        private List<byte> GetPressedButtons(byte[] rgbButtons)
        {
            var pressedButtons = new List<byte>();
            for (byte butId = 0; butId < rgbButtons.Length; butId++)
            {
                if (rgbButtons[butId] > 0)
                {
                    pressedButtons.Add(butId);
                }
            }

            return pressedButtons;
        }

        private bool _logiInitialized = false;
        public void InitializeLogi()
        {
            if (!_logiInitialized)
            {
                LogitechGSDK.LogiSteeringInitialize(true);
                _logiInitialized = true;
            }
        }

        public void SetLateral(float lateral)
        {
            InitializeLogi();

            if (!Cruiser.Parameters.AutoDrive)
            {
                OutputControl.SetLateral(lateral);
                LogitechGSDK.LogiStopSpringForce(0);
                return;
            }

            if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
            {
                if (lateral == 0)
                {
                    LogitechGSDK.LogiStopSpringForce(0);
                }
                else
                {
                    LogitechGSDK.LogiPlaySpringForce(0, (int)(lateral * _maxOffset), 80, 80);
                }
            }
        }

        public void SetLongitudal(float longitudal)
        {
            if (Cruiser.Parameters.AutoDrive)
                OutputControl.SetLongitudal(longitudal);
        }

        public void ShiftUp()
        {
            if (Cruiser.Parameters.AutoDrive)
                OutputControl.ShiftUp();
        }

        public void ShiftDown()
        {
            if (Cruiser.Parameters.AutoDrive)
                OutputControl.ShiftDown();
        }

        public void Ignition()
        {
            if (Cruiser.Parameters.AutoDrive)
                OutputControl.Ignition();
        }

        public void Reset()
        {
            OutputControl.Reset();
        }

        public void Dispose()
        {
            _logiTaskRun = false;
            OutputControl.Dispose();
            LogitechGSDK.LogiSteeringShutdown();
        }
    }
}
