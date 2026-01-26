//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: CyberKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: 
//	History			: 
//////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace net_wpf_audioplayer.module {
    public class NotifyPropChanged : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

		protected void DoNotify(string propertyName = "") {
			PropertyChanged?.Invoke(this, new(propertyName));
		}


		protected void DoNotifyUI(string propertyName = "") {
			if (Application.Current == null)
				return;
			Application.Current.Dispatcher.BeginInvoke(() => {
				PropertyChanged?.Invoke(this, new(propertyName));
			}, System.Windows.Threading.DispatcherPriority.DataBind);
		}
	}
}
