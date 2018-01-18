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
    public class VJoyControl : IControl, IDisposable
    {
        static public uint id = 1;
        private vJoy joy;

        public VJoyControl()
        {
            InitVJoystick();
            Reset();
        }

        private void InitVJoystick()
        {
            joy = new vJoy();

            if (!joy.vJoyEnabled())
            {
                throw new NotSupportedException("vJoy not enabled!");
            }

            VjdStat status = joy.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    //Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    //Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    throw new NotSupportedException("vJoy Device is already owned by another feeder");
                case VjdStat.VJD_STAT_MISS:
                    throw new NotSupportedException("vJoy Device is not installed or disabled");
                default:
                    throw new NotSupportedException("vJoy Device general error");
            };

            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joy.AcquireVJD(id))))
            {
                throw new NotSupportedException("Failed to acquire vJoy device");
            }

            joy.ResetVJD(id);
        }



        public void Reset()
        {
            joy.SetAxis(0, id, HID_USAGES.HID_USAGE_X);
            joy.SetAxis(0, id, HID_USAGES.HID_USAGE_Y);
        }

        private static int halfAxis = Int16.MaxValue / 2;
        public void SetLongitudal(float longitudal)
        {
            var limitedVal = Math.Min(1f, Math.Max(-1f, longitudal));
            var val = (int)(limitedVal * halfAxis) + halfAxis;
            joy.SetAxis(val, id, HID_USAGES.HID_USAGE_Y);
        }

        public void SetLateral(float lateral)
        {
            lateral *= 2;
            var limitedVal = Math.Min(1f, Math.Max(-1f, lateral));
            var val = (int)(limitedVal * halfAxis) + halfAxis;
            joy.SetAxis(val, id, HID_USAGES.HID_USAGE_X);
        }

        public void Dispose()
        {
            joy.RelinquishVJD(id);
        }

        public void ShiftUp()
        {
            PressBtn(1);
        }

        public void ShiftDown()
        {
            PressBtn(2);
        }

        public void Ignition()
        {
            PressBtn(3);
        }

        private void PressBtn(uint keyId)
        {
            new Task(() =>
            {
                joy.SetBtn(true, id, keyId);
                Thread.Sleep(50);
                joy.SetBtn(false, id, keyId);
            }).Start();
        }

    }
}
