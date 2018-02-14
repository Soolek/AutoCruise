﻿using SharpDX.DirectInput;
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
        public bool Initialized { get; private set; }

        private IControl _outputControl { get; set; }
        private Parameters _parameters { get; set; }

        private int _maxOffset = 80;
        private bool _logiTaskRun = true;
        private Task _logiTask;

        private float? _inputLongitudal;
        private float? _inputLateral;

        public FfbWheelIntermediaryControl(IControl outputControl, Parameters parameters)
        {
            _outputControl = outputControl;
            _parameters = parameters;

            InitializeLogi();

            if (Initialized)
            {
                _logiTask = new Task(() =>
                {
                    Thread.Sleep(2000);

                    while (_logiTaskRun)
                    {
                        if (Initialized && LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
                        {
                            var state = LogitechGSDK.LogiGetStateCSharp(0);
                            DoWheelControl(state);
                        }
                        Thread.Sleep(1);
                    }
                });
                _logiTask.Start();
            }
        }

        private void DoWheelControl(LogitechGSDK.DIJOYSTATE2ENGINES state)
        {
            float lateral = ((float)state.lX / Int16.MaxValue) / (_maxOffset / 100f);
            _outputControl.SetLateral(lateral);

            float acc = (float)(-state.lY + Int16.MaxValue) / (2 * Int16.MaxValue);
            float brake = (float)(-state.lRz + Int16.MaxValue) / (2 * Int16.MaxValue);
            float pedalsLongitudal = acc - brake;
            _outputControl.SetLongitudal(pedalsLongitudal != 0 ? pedalsLongitudal : _inputLongitudal ?? 0);

            var pressedButtons = GetPressedButtons(state.rgbButtons);
            var justPressedButtons = GetJustPressedButtons(pressedButtons).ToList();

            if (justPressedButtons.Contains(10))
            {
                _outputControl.ShiftDown();
            }
            if (justPressedButtons.Contains(11))
            {
                _outputControl.ShiftUp();
            }

            if (_inputLateral != null)
            {
                LogitechGSDK.LogiStopDamperForce(0);
                LogitechGSDK.LogiPlaySpringForce(0, (int)(_inputLateral * _maxOffset), 80, 80);
            }
            else
            {
                int damper = (int)(Math.Max(0, 0.5f - _parameters.Speed) * 25);
                if (damper > 0)
                {
                    LogitechGSDK.LogiStopSpringForce(0);
                    LogitechGSDK.LogiPlayDamperForce(0, damper);
                }
                else
                {
                    LogitechGSDK.LogiStopDamperForce(0);
                    int centeringForce = Math.Min(90, (int)(_parameters.Speed * 4));
                    LogitechGSDK.LogiPlaySpringForce(0, 0, centeringForce, centeringForce);
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

        public void InitializeLogi()
        {
            if (!Initialized)
            {
                Initialized = LogitechGSDK.LogiSteeringInitialize(true);
            }
        }

        public void SetLateral(float? lateral)
        {
            _inputLateral = lateral;
        }

        public void SetLongitudal(float? longitudal)
        {
            _inputLongitudal = longitudal;
        }

        public void ShiftUp()
        {
            _outputControl.ShiftUp();
        }

        public void ShiftDown()
        {
            _outputControl.ShiftDown();
        }

        public void Ignition()
        {
            _outputControl.Ignition();
        }

        public void Reset()
        {
            _outputControl.Reset();
        }

        public void Dispose()
        {
            _logiTaskRun = false;
            LogitechGSDK.LogiSteeringShutdown();

            if (Initialized)
            {
                _outputControl.Dispose();
            }
        }
    }
}
