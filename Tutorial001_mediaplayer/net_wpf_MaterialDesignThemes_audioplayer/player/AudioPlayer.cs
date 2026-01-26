//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: CyberKDH(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: 
//	History			: 
//////////////////////////////////////////////////////////////////////////////////////////////////

using FFmpeg.AutoGen;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using net_wpf_audioplayer.module;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace net_wpf_audioplayer.player {
    public unsafe class AudioPlayer : NotifyPropChanged, IDisposable {
		public static readonly string TAG = "[AudioPlayer]";

		private AVFormatContext* m_avformatcontext = null;
		private AVCodec* m_pCodec = null;
		private AVCodecParameters* m_pCodecPar = null;
		private AVCodecContext* m_pCodecCtx = null;
		private int m_nAudioStream_ID = -1;
		private SwrContext* m_pSWR = null;
		private AVChannelLayout* m_pOut_channelLayout = null;
		private string m_strFileOrUri = "";
		private long m_dbl_duration = 0;
		private BufferedWaveProvider? m_buffer_audio_out = null;
		private WasapiOut? m_audio_out = null;
		private WaveFormat m_audio_out_waveformat = new WaveFormat(48000, 16, 2);
		private Thread? m_thread_decode = null;
		private ManualResetEvent m_evtStop = new ManualResetEvent(false);
		private TSVar<double> m_dbl_cur = new TSVar<double>(0);
		private E_PLAYSTATE m_curState = E_PLAYSTATE.STOPPED;
		private bool m_bLoaded = false;
		private double m_dblSeekStart = 0;
		private volatile bool m_bSeekRequested = false;
		private TSVar<double> m_dblSeekReqSec = new TSVar<double>(0);
		private float m_fCurVolume = 0f;

        public AudioPlayer() {

		}

		// post xaml start
		public double Cur_Pos {
			get { return GetCurSec(); }
		}

		public double Cur_Duration {
			get { return GetDurationSecs(); }
		}
		// post xaml end

		public long GetDuration() {
			return m_dbl_duration;
		}

		public double GetDurationSecs() {
			return (double)((double)m_dbl_duration / (double)ffmpeg.AV_TIME_BASE);
		}

		public double GetCurSec() {
			return m_dbl_cur.Value; // + m_dblSeekStart;
		}

		public int GetCurPrecent() {
			// m_dbl_duration : 100 = m_dbl_cur.Value ; x
			return (int)((m_dbl_cur.Value/* + m_dblSeekStart*/) * 100 / GetDurationSecs());
		}

		public E_PLAYSTATE GetPlayState() {
			return m_curState;
		}

		public bool IsLoaded() {
			return m_bLoaded;
		}

		public float GetVolume() {
			float fret = -1;
			if (m_audio_out != null) {
				float[] fvolume = m_audio_out.AudioStreamVolume.GetAllVolumes();
				fret = fvolume[0];
			}
			return fret;
		}

		protected void __Free() {

			if (m_pCodecCtx != null) {
				fixed (AVCodecContext** p1 = &m_pCodecCtx) {
					ffmpeg.avcodec_free_context(p1);
				}
				m_pCodecCtx = null;
			}

			if (m_pSWR != null) {
				fixed (SwrContext** p1 = &m_pSWR) {
					ffmpeg.swr_free(p1);
				}
				m_pSWR = null;
			}

			if (m_avformatcontext != null) {
				fixed (AVFormatContext** p1 = &m_avformatcontext) {
					ffmpeg.avformat_close_input(p1);
				}

				ffmpeg.avformat_free_context(m_avformatcontext);
				m_avformatcontext = null;
			}

			m_buffer_audio_out?.ClearBuffer();
			m_buffer_audio_out = null;



		}

		public void Dispose() {
			__Free();
		}

		public bool Load(string strfileoruri) {
			int a1 = 0;
			bool bret = false;
			m_strFileOrUri = strfileoruri;
			int nret = -1;
			int i;

			m_bLoaded = false;

			__Free();

			try {
				while (true) {
					AVFormatContext* avformatcontext = ffmpeg.avformat_alloc_context();
					nret = ffmpeg.avformat_open_input(&avformatcontext, strfileoruri, null, null);
					if (nret != 0) {
						break;
					}
					m_avformatcontext = avformatcontext;

#if true
					nret = ffmpeg.avformat_find_stream_info(m_avformatcontext, null);
					if (nret != 0) {
						break;
					}
#endif

					ffmpeg.av_dump_format(m_avformatcontext, 0, strfileoruri, 0);
					m_dbl_duration = avformatcontext->duration;

					for (i = 0; i < avformatcontext->nb_streams; i++) {
						if (avformatcontext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO) {
							m_nAudioStream_ID = i;
							break;
						}
					}

					if (m_nAudioStream_ID == -1) {
						break;
					}

					m_pCodec = (AVCodec*)ffmpeg.avcodec_find_decoder(avformatcontext->streams[m_nAudioStream_ID]->codecpar->codec_id);
					if (m_pCodec == null) {
						break;
					}

					m_pCodecPar = avformatcontext->streams[m_nAudioStream_ID]->codecpar;
					m_pCodecCtx = ffmpeg.avcodec_alloc_context3(m_pCodec);
					ffmpeg.avcodec_parameters_to_context(m_pCodecCtx, m_pCodecPar);

					if (ffmpeg.avcodec_open2(m_pCodecCtx, m_pCodec, null) < 0) {
						break;
					}

					AVChannelLayout channelLayout;
					ffmpeg.av_channel_layout_default(&channelLayout, 2);

					SwrContext* swr = null;
					nret = ffmpeg.swr_alloc_set_opts2(
						&swr,
						&channelLayout,
						AVSampleFormat.AV_SAMPLE_FMT_S16,
						48000,
						&m_pCodecCtx->ch_layout,
						m_pCodecCtx->sample_fmt,
						m_pCodecCtx->sample_rate,
						0,
						null);
					m_pSWR = swr;
					nret = ffmpeg.swr_init(swr);
					if (nret != 0) {
						break;
					}

					m_curState = E_PLAYSTATE.READY;
					m_bLoaded = true;
					DoNotifyUI("loaded");
					DoNotifyUI("Cur_Duration");


					m_dbl_cur.Value = 0;
					m_dblSeekStart = 0;

					bret = true;
					break;
				}
			}
			catch (Exception ex) {
				bret = false;
			}

			if (bret == false) {
				__Free();
			}


			return bret;
		}

		public void Play() {
			if (m_curState == E_PLAYSTATE.PAUSING) {
				m_audio_out.Play();
				m_curState = E_PLAYSTATE.PLAYING;
				DoNotifyUI("played");
			}
			else {
				DoStartDecode(true, 0);
				m_curState = E_PLAYSTATE.PLAYING;
				DoNotifyUI("played");
			}
		}

		public void Pause() {
			if (m_bLoaded == true) {
				if (m_curState == E_PLAYSTATE.PLAYING) {
					m_audio_out.Pause();
				}
			}
			m_curState = E_PLAYSTATE.PAUSING;
			DoNotifyUI("paused");
		}

		public void Stop() {
			DoStartDecode(false, 0);
			m_curState = E_PLAYSTATE.STOPPED;
			DoNotifyUI("stopped");
		}

		public void Seek(double dblsecs) {
			int a1 = 0;
			Debug.WriteLine($"{TAG}[seek] = {dblsecs}");

			if (m_thread_decode != null) {
				m_dblSeekReqSec.Value = dblsecs;
				m_bSeekRequested = true;
			}
			else {
				DoStartDecode(true, dblsecs);
				m_curState = E_PLAYSTATE.PLAYING;
				DoNotifyUI("played");
			}
		}

		public void SetVolume(float fcurvol) {
			if (m_audio_out != null) {
				float[] fvolume = m_audio_out.AudioStreamVolume.GetAllVolumes();
				int i;
				for (i = 0; i < fvolume.Length; i++) {
					fvolume[i] = fcurvol;
				}
				m_audio_out.AudioStreamVolume.SetAllVolumes(fvolume);
				m_fCurVolume = fcurvol;
			}
		}

		public void Mute(bool bmute) {
			float fvol = GetVolume();
			if (fvol == -1) {
				return;
			}

			if (bmute == true) {
				m_fCurVolume = fvol;
				SetVolume(0);
			}
			else {
				SetVolume(m_fCurVolume);
			}
		}

		public bool IsPlaying() {
			return (m_curState == E_PLAYSTATE.PLAYING) || (m_curState == E_PLAYSTATE.PAUSING) || m_thread_decode != null;
		}

		protected void DoStartDecode(bool bstart, double dblstartsec) {
			if (m_thread_decode != null) {
				m_evtStop.Set();
				m_thread_decode.Join();
				m_thread_decode = null;

				m_audio_out?.Stop();
				m_audio_out?.Dispose();
				m_audio_out = null;

				m_buffer_audio_out?.ClearBuffer();
				m_buffer_audio_out = null;
			}

			if (bstart == true) {
				m_evtStop.Reset();

				/*
				double dblspeed = 5.0f;
				int nchannel = m_pCodecCtx->ch_layout.nb_channels;

				m_audio_out_waveformat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm,
					(int)(m_pCodecCtx->sample_rate * dblspeed),
					nchannel,
					(int)(m_pCodecCtx->sample_rate * dblspeed) * nchannel * 2,
					nchannel * 2,
					16);
				*/

				m_buffer_audio_out = new BufferedWaveProvider(m_audio_out_waveformat) {
					//DiscardOnBufferOverflow = true
					//BufferLength = 1024 * 1024 * 5
				};

				m_audio_out = new WasapiOut(AudioClientShareMode.Shared, true, 20);
				m_audio_out.Init(m_buffer_audio_out);
				m_audio_out.Play();

				m_thread_decode = new Thread(() => {
					DoDecodeThread(dblstartsec);
				});
				m_thread_decode.Start();
			}

		}

		protected void DoDecodeThread(double dblstartpos) {
			int a1 = 0;
			AVPacket* pPacket = null;
			AVFrame* pFrame = null;

			pPacket = ffmpeg.av_packet_alloc();
			pFrame = ffmpeg.av_frame_alloc();
			byte[] managedBuffer = new byte[192000];
			byte** outArr = stackalloc byte*[1];

			AVRational avrational_time_base = m_pCodecCtx->time_base;
			AVRational aVRational_stram_time_base = m_avformatcontext->streams[m_nAudioStream_ID]->time_base;

			m_dblSeekStart = dblstartpos;

			float[] fvolume = m_audio_out.AudioStreamVolume.GetAllVolumes();
			DoNotifyUI("volume chaned");

			int nret = 0;
			long targetTimestamp = 0;
#if false
			long startTs = m_avformatcontext->streams[m_nAudioStream_ID]->start_time;

			if (startTs == ffmpeg.AV_NOPTS_VALUE) {
				startTs = 0;
			}

#endif

#if true
			targetTimestamp = (long)((long)dblstartpos * aVRational_stram_time_base.den);

			nret = ffmpeg.av_seek_frame(
				m_avformatcontext,
				m_nAudioStream_ID,
				targetTimestamp,
				ffmpeg.AVSEEK_FLAG_BACKWARD
			);

			if (nret >= 0) {
				ffmpeg.avcodec_flush_buffers(m_pCodecCtx);
				ffmpeg.swr_init(m_pSWR);
			}
#endif

#if false

			ffmpeg.swr_set_compensation(
				m_pSWR,
				(int)((1.0 - 2.5) * m_pCodecCtx->sample_rate),
				m_pCodecCtx->sample_rate
			);

#endif




			int neofproc = 0;

			while (m_evtStop.WaitOne(0) == false) {

				if (m_bSeekRequested == true) {
					m_bSeekRequested = false;

					m_dblSeekStart = m_dblSeekReqSec.Value;
					targetTimestamp = (long)((long)m_dblSeekStart * aVRational_stram_time_base.den);

					nret = ffmpeg.av_seek_frame(
						m_avformatcontext,
						m_nAudioStream_ID,
						targetTimestamp,
						ffmpeg.AVSEEK_FLAG_BACKWARD
					);

					if (nret >= 0) {
						m_audio_out.Stop();
						m_audio_out.Play();
						ffmpeg.avcodec_flush_buffers(m_pCodecCtx);
						ffmpeg.swr_init(m_pSWR);
						m_buffer_audio_out.ClearBuffer();
					}
				}

				nret = ffmpeg.av_read_frame(m_avformatcontext, pPacket);
				if (nret >= 0) {
					if (pPacket->stream_index != m_nAudioStream_ID) {
						ffmpeg.av_packet_unref(pPacket);
						continue;
					}

					ffmpeg.avcodec_send_packet(m_pCodecCtx, pPacket);
				}
				else if (nret < 0) {
					break;
#if false

					neofproc = 1;
					nret = ffmpeg.avcodec_send_packet(m_pCodecCtx, null);
					if(nret < 0) {
						break;
					}
					else {
						a1++;
					}
#endif
				}

				if (m_evtStop.WaitOne(0) == true) {
					break;
				}

				while (m_evtStop.WaitOne(0) == false && m_bSeekRequested == false) {
					nret = ffmpeg.avcodec_receive_frame(m_pCodecCtx, pFrame);
					if (nret == ffmpeg.AVERROR_EOF) {
						break;
					}
					else if (nret == ffmpeg.AVERROR(ffmpeg.EAGAIN)) {
						break;
					}
					else if (nret < 0) {
						break;
					}

					Console.WriteLine($"= timestamp: {pFrame->best_effort_timestamp * ffmpeg.av_q2d(aVRational_stram_time_base)}");

					TimeSpan ts = m_audio_out.GetPositionTimeSpan();
					m_dbl_cur.Value = ts.TotalSeconds + m_dblSeekStart;
					DoNotifyUI("Cur_Pos");

					while (m_buffer_audio_out.BufferedBytes > m_buffer_audio_out.BufferLength / 2 && m_evtStop.WaitOne(0) == false && m_bSeekRequested == false) {
						System.Threading.Thread.Sleep(50);
					}

					if (m_bSeekRequested == true) {
						break;
					}

					int nsamples = 0;

					fixed (byte* outPtr = managedBuffer) {

						outArr[0] = outPtr;

						nsamples = ffmpeg.swr_convert(
							m_pSWR,
							outArr,
							pFrame->nb_samples,
							pFrame->extended_data,
							pFrame->nb_samples);

						if (nsamples > 0) {
							int bytes = nsamples * 2 * 2;
							m_buffer_audio_out.AddSamples(managedBuffer, 0, bytes);
							//Console.WriteLine($"= {output.PlaybackState}");
						}
						else {
							break;
						}

						nsamples = ffmpeg.swr_convert(
							m_pSWR,
							outArr,
							pFrame->nb_samples,
							null,
							0);

						if (nsamples > 0) {
							int bytes = nsamples * 2 * 2;
							m_buffer_audio_out.AddSamples(managedBuffer, 0, bytes);
							//Console.WriteLine($"= {output.PlaybackState}");
						}
						else {
							break;
						}
					}

					ffmpeg.av_frame_unref(pFrame);
				}

				ffmpeg.av_packet_unref(pPacket);
			}

			while (m_buffer_audio_out.BufferedBytes > 0 && m_evtStop.WaitOne(0) == false && m_bSeekRequested == false) {
				Thread.Sleep(10);
				TimeSpan ts = m_audio_out.GetPositionTimeSpan();
				m_dbl_cur.Value = ts.TotalSeconds + m_dblSeekStart;
				DoNotifyUI("Cur_Pos");
			}

			if (m_evtStop.WaitOne(0) == false) {
				m_curState = E_PLAYSTATE.ENDED;
				DoNotifyUI("ended");
			}

			//m_buffer_audio_out.ClearBuffer();
			m_thread_decode = null;
		}
	}
}
