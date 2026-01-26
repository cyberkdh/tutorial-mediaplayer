//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: CyberKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: 
//	History			: 
//////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace net_wpf_audioplayer.module {

    // singleton class
    public class AppDefine {
        private static AppDefine g_ad = null;

        public static AppDefine Get() {
            if(g_ad == null) {
                g_ad = new AppDefine();
            }
            return g_ad;
        }

        protected AppDefine() {

        }

        // Get Current Module Base Path
		public string GetPGMBasePath() {
			// return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			return System.IO.Path.GetDirectoryName(Environment.ProcessPath);
		}

		public bool DoOpenMediaFileDialog(Window parentwnd, out string strSelectedPath, string strTitle = "Select media file") {
			strSelectedPath = "";
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Media Files|" + "*.wav;*.mp3;*.mp4;*.flc;*.flv;*.avi;|All Files(*.*)|*.*";
			dlg.Title = strTitle;
			bool bret = dlg.ShowDialog(parentwnd) == true ? true : false;
			if (bret == true) {
				strSelectedPath = dlg.FileName;
			}
			return bret;
		}

		public static void DoNotify(Action action) {
			if (Application.Current == null)
				return;
			Application.Current.Dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.DataBind);
		}
	}

	public static class WindowExtensions {
		// from winuser.h
		private const int GWL_STYLE = -16,
						  WS_MAXIMIZEBOX = 0x10000,
						  WS_MINIMIZEBOX = 0x20000;

		[DllImport("user32.dll")]
		public extern static int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll")]
		public extern static int SetWindowLong(IntPtr hwnd, int index, int value);

		public static void HideMinimizeAndMaximizeButtons(this Window window) {
			IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
			var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

			SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
		}
	}

}
