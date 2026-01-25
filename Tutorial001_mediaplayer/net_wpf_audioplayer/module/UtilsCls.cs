//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: CyberKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: 
//	History			: 
//////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace net_wpf_audioplayer.module {
	public class TSVar<T> {

		private object m_objlock = new object();
		private T? m_value;

		public T Value {
			get {
				T retval;
				lock (m_objlock) {
					retval = m_value;
				}
				return retval;
			}
			set {
				lock (m_objlock) {
					m_value = value;
				}
			}
		}

		public TSVar(T val) {
			Value = val;
		}

	}

	public class SecsToTimeConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var ts = TimeSpan.FromSeconds((double)value);

			string strret = "";

			if (ts.TotalHours > 9) {
				strret = ((int)ts.TotalHours) + ts.ToString(@"\:mm\:ss");
			}
			else {
				strret = ts.ToString(@"hh\:mm\:ss");
			}

			//Debug.WriteLine($"= [SecsToTimeConverter] Convert: {strret}");
			return (string)strret;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}
