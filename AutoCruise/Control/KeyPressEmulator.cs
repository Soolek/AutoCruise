using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoCruise.Control
{
    public class KeyPressEmulator : IControl, IDisposable
    {
        const UInt32 WM_KEYUP = 0x0101;
        const UInt32 WM_KEYDOWN = 0x0100;

        const int VK_LEFT = 0x25;
        const int VK_UP = 0x26;
        const int VK_RIGHT = 0x27;
        const int VK_DOWN = 0x28;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        IntPtr _gamehWnd = IntPtr.Zero;

        public KeyPressEmulator()
        {
            _gamehWnd = FindWindow(null, "Live for Speed");
        }

        public void Reset()
        {
            _lastLongitudal = 0;
            _lastLateral = 0;
        }

        private static float longitudalTreshold = 0.4f;
        private float _lastLongitudal = 0;
        public void SetLongitudal(float? longitudal)
        {
            if (longitudal==null || Math.Abs(longitudal.Value) < longitudalTreshold)
            {
                longitudal = 0;
            }

            if (Math.Sign(longitudal.Value) != Math.Sign(_lastLongitudal))
            {
                PostMessage(_gamehWnd, WM_KEYUP, VK_UP, 0);
                PostMessage(_gamehWnd, WM_KEYUP, VK_DOWN, 0);

                if (longitudal > longitudalTreshold)
                    PostMessage(_gamehWnd, WM_KEYDOWN, VK_UP, 0);
                else if (longitudal < -longitudalTreshold)
                    PostMessage(_gamehWnd, WM_KEYDOWN, VK_DOWN, 0);

                _lastLongitudal = longitudal.Value;
            }
        }

        private static float lateralTreshold = 0.3f;
        private float _lastLateral = 0;
        public void SetLateral(float? lateral)
        {
            if(lateral==null || Math.Abs(lateral.Value) < lateralTreshold)
            {
                lateral = 0;
            }

            if (Math.Sign(lateral.Value) != Math.Sign(_lastLateral))
            {
                PostMessage(_gamehWnd, WM_KEYUP, VK_LEFT, 0);
                PostMessage(_gamehWnd, WM_KEYUP, VK_RIGHT, 0);

                if (lateral > lateralTreshold)
                    PostMessage(_gamehWnd, WM_KEYDOWN, VK_RIGHT, 0);
                else if (lateral < -lateralTreshold)
                    PostMessage(_gamehWnd, WM_KEYDOWN, VK_LEFT, 0);

                _lastLateral = lateral.Value;
            }
        }

        public void Dispose()
        {
            PostMessage(_gamehWnd, WM_KEYUP, VK_LEFT, 0);
            PostMessage(_gamehWnd, WM_KEYUP, VK_UP, 0);
            PostMessage(_gamehWnd, WM_KEYUP, VK_RIGHT, 0);
            PostMessage(_gamehWnd, WM_KEYUP, VK_DOWN, 0);
        }

        public void ShiftUp()
        {
            throw new NotImplementedException();
        }

        public void ShiftDown()
        {
            throw new NotImplementedException();
        }

        public void Ignition()
        {
            throw new NotImplementedException();
        }
    }
}
