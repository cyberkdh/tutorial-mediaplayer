//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: CyberKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: 
//	History			: 
//////////////////////////////////////////////////////////////////////////////////////////////////

using net_wpf_audioplayer.module;
using net_wpf_audioplayer.player;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace net_wpf_audioplayer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
		private string m_strTitle = "DH:Media Player";
		private AudioPlayer m_audioplayer = new AudioPlayer();

		private bool m_bForeceClose = false;
		private TSVar<bool> m_bTracking = new TSVar<bool>(false);

		public MainWindow() {
            InitializeComponent();

			this.DataContext = m_audioplayer;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			this.Title = m_strTitle;
			this.ShowInTaskbar = true;

			this.HideMinimizeAndMaximizeButtons();

			m_audioplayer.PropertyChanged += M_audioplayer_PropertyChanged;

			m_slider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((object sender, DragStartedEventArgs e) => {
				int a1 = 0;
				a1++;
				Debug.WriteLine($"= [DragStartedEvent] ");
				m_bTracking.Value = true;

			}));

			m_slider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((s, e) => {

				Debug.WriteLine($"= [DragCompletedEvent] {e.Canceled}");

				if (e.Canceled == false) {
					m_bTracking.Value = false;

					double dbltotalsecs = m_audioplayer.GetDurationSecs();

					// dbltotal : 100 = x : pos

					double dblcursecs = (double)((double)m_slider.Value * dbltotalsecs / (double)100.0);
					Debug.WriteLine($"= [DragCompletedEvent] pos: {m_slider.Value}, secs: {dblcursecs}");

					m_audioplayer.Seek(dblcursecs);

				}


			}));

			m_slider.ValueChanged += (s, e) => {
				if (m_bTracking.Value == true) {
					double dbltotalsecs = m_audioplayer.GetDurationSecs();

					// dbltotal : 100 = x : pos

					double dblcursecs = (double)((double)m_slider.Value * dbltotalsecs / (double)100.0);
					Debug.WriteLine($"= pos: {m_slider.Value}, secs: {dblcursecs}");
					string strtime = new SecsToTimeConverter().Convert((double)dblcursecs, typeof(string), null, null).ToString();
					//m_lbl_CurInfo.Content = strtime;
				}
			};

			m_slider_volume.ValueChanged += (s, e) => {
				double dblval = m_slider_volume.Value;
				m_audioplayer.SetVolume((float)(dblval / 100));
			};

#if DEBUG
			//LoadMedia(@"H:\temp_chan\a.mp3");
#endif

		}

		private void M_audioplayer_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
				if (e.PropertyName.CompareTo("Cur_Pos") == 0) {

					/*
					if (m_bTracking.Value == false) {
						double dblcursecs = m_audioplayer.GetCurSec();
						//Debug.WriteLine($"= dblcursecs: {dblcursecs}");
						string strtime = new SecsToTimeConverter().Convert(dblcursecs, typeof(string), null, null).ToString();
						m_lbl_CurInfo.Content = strtime;

						int npercent = m_audioplayer.GetCurPrecent();
						//Debug.WriteLine($"= percent: {npercent}");

						m_slider.Value = npercent;
					}
					*/

				}
				else if (e.PropertyName.CompareTo("stopped") == 0) {
					if (m_bForeceClose == true) {
						//m_audioplayer?.Dispose();
						this.Close();
						return;
					}
				}
				else if (e.PropertyName.CompareTo("loaded") == 0) {
					//string strtime = new SecsToTimeConverter().Convert(m_audioplayer.GetDurationSecs(), typeof(string), null, null).ToString();
					//m_lbl_TotalInfo.Content = strtime;
				}
				else if (e.PropertyName.CompareTo("volume chaned") == 0) {
					float fcurvolume = m_audioplayer.GetVolume();
					int nval = (int)(fcurvolume * 100);
					m_slider_volume.Value = nval;
				}

				E_PLAYSTATE eplaystate = m_audioplayer.GetPlayState();
				if (eplaystate == E_PLAYSTATE.PLAYING) {
					m_btnPlay.Content = "Pause";
				}
				else if (eplaystate == E_PLAYSTATE.PAUSING || eplaystate == E_PLAYSTATE.ENDED) {
					m_btnPlay.Content = "play";
				}

			}));
		}

		protected void LoadMedia(string strfileoruri) {
			m_btnPlay.IsEnabled = false;
			if (m_audioplayer.IsPlaying() == true) {
				m_audioplayer.Stop();
			}

			if (m_audioplayer.Load(strfileoruri) == false) {
				MessageBox.Show(this, $"failed to load media file: {strfileoruri}", "", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			else {
				m_btnPlay.IsEnabled = true;

				this.Title = $"{m_strTitle}: {strfileoruri}";
				m_audioplayer.Play();
			}
		}

		private void OnProcControls(object sender, RoutedEventArgs e) {
			if(sender == m_btnOpen) {
				string strselectedfilepath = "";
				if (AppDefine.Get().DoOpenMediaFileDialog(this, out strselectedfilepath) == true) {
					LoadMedia(strselectedfilepath);
				}
			}
			else if(sender == m_btnPlay) {
				if (m_audioplayer.IsLoaded() == true) {
					if (m_audioplayer.GetPlayState() == E_PLAYSTATE.PLAYING) {
						m_audioplayer.Pause();
					}
					else {
						m_audioplayer.Play();
					}
				}
			}
			else if (sender == m_btnVolumeMute) {
				float fcurvolume = m_audioplayer.GetVolume();
				if (fcurvolume != 0) {
					m_audioplayer.Mute(true);
				}
				else {
					m_audioplayer.Mute(false);
				}
			}
		}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (m_audioplayer.IsPlaying() == true) {
				m_bForeceClose = true;
				e.Cancel = true;

				//m_audioplayer.PropertyChanged -= M_audioplayer_PropertyChanged;


				Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
					m_audioplayer.Stop();
				}));

			}
		}

        
	}
}