using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

namespace AutoCruise.Control
{
    public class FfbWheelIntermediaryControl : IControl
    {
        public IControl OutputControl { get; private set; }
        public Joystick Wheel { get; private set; }

        private Guid _wheelGuid;
        private Effect _wheelEffect;

        public FfbWheelIntermediaryControl(IControl outputControl)
        {
            OutputControl = outputControl;

            var directInput = new DirectInput();
            var device = directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.ForceFeedback).FirstOrDefault();
            _wheelGuid = device.InstanceGuid;
            Wheel = new Joystick(directInput, _wheelGuid);

            MessageBox.Show(string.Format("Wheel '{0}' found with ffb: {1}", device.ProductName, Wheel.GetEffects().Select(e => e.Name).Aggregate((a, b) => a + "," + b)));
            //wheel.Properties.BufferSize=128;
        }

        private bool _initialized = false;
        public void Initialize()
        {
            if(_initialized)
            {
                return;
            }
            Wheel.SetCooperativeLevel(new WindowInteropHelper(Application.Current.MainWindow).Handle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            Wheel.Acquire();
            _initialized = true;
        }

        public void SetLateral(float lateral)
        {
            Initialize();
            if (lateral == 0 && _wheelEffect != null)
            {
                _wheelEffect.Stop();
            }
            else
            {
                var effectParameters = new EffectParameters()
                {
                    Directions = new[] { 0 },
                    Axes = new[] { 0 },
                    Flags = EffectFlags.Cartesian | EffectFlags.ObjectOffsets,
                    Duration = int.MaxValue,
                    SamplePeriod = 0,
                    Gain = 10000,
                    Envelope = null,
                    Parameters = new ConstantForce()
                    {
                        Magnitude = -5000
                    }
                };
                _wheelEffect = new Effect(Wheel, _wheelGuid, effectParameters);
                _wheelEffect.Start(1, EffectPlayFlags.None);
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
        }
    }
}
