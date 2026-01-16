using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace music_editer.Api
{
    class SetWindowText_W
    {
        public static void SetWindowText(IntPtr window, string window_name)
        {
            [DllImport("user32.dll")]
            static extern bool SetWindowText(IntPtr hWnd, string lpString);

            if (window != IntPtr.Zero)
            {
                SetWindowText(window, window_name);
            }
        }
    }
}
