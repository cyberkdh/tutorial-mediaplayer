//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: CyberKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: 
//	History			: 
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////

using FFmpeg.AutoGen;
using net_wpf_audioplayer.module;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace net_wpf_audioplayer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void Application_Startup(object sender, StartupEventArgs e) {            
            // assume the ffmpeg dlls are in the x64 subfolder of the exe module
            ffmpeg.RootPath = Path.Combine(AppDefine.Get().GetPGMBasePath(), "ffmpeg_x64");
		}
	}

}
