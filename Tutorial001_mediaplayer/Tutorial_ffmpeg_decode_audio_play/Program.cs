//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: 경덕현(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: DHApp.common
//	History			: 
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////

//
// Decode Audio from BigBuckBunny.mp4
// And Play

using FFmpeg.AutoGen;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;

namespace Tutorial_ffmpeg_decode_audio_play {
    internal unsafe class Program {

		public static AVFormatContext* m_avformatcontext = null;
		public static int m_nAudioStream_ID = -1;
		public static AVCodec* m_pAudio_Codec = null;
		public static AVCodecParameters* m_pAudio_CodecPar = null;
		public static AVCodecContext* m_pAudio_CodecCtx = null;
		public static SwrContext* m_pAudio_SWR = null;
		public static AVChannelLayout m_audio_channel_layout = new AVChannelLayout();
		public static long m_dbl_duration = 0;
		public static BufferedWaveProvider? m_buffer_audio_out = null;
		public static WasapiOut? m_audio_out = null;
		public static WaveFormat m_audio_out_waveformat = new WaveFormat(48000, 16, 2);
		public static bool m_bInitAudio = false;

		static void Main(string[] args) {
			Console.WriteLine("Decode Start: BigBuckBunny.mp4");

			// Set ffmpeg DLL Path and audio file
			// please check the lcoations of ffmpeg and mp4 file
			ffmpeg.RootPath = Path.Combine(GetPGMBasePath(), "ffmpeg_x64");
			PlayAudio(Path.Combine(GetPGMBasePath(), "BigBuckBunny.mp4"));

			Free();

			Console.WriteLine("Decode End: BigBuckBunny.mp4");
		}

		public static string GetPGMBasePath() {
			// return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			return System.IO.Path.GetDirectoryName(Environment.ProcessPath);

		}

		static void Free() {
			// Free
			if (m_pAudio_CodecCtx != null) {
				fixed (AVCodecContext** p1 = &m_pAudio_CodecCtx) {
					ffmpeg.avcodec_free_context(p1);
				}
				m_pAudio_CodecCtx = null;
			}

			if (m_avformatcontext != null) {
				fixed (AVFormatContext** p1 = &m_avformatcontext) {
					ffmpeg.avformat_close_input(p1);
				}
				ffmpeg.avformat_free_context(m_avformatcontext);
				m_avformatcontext = null;
			}

			if (m_pAudio_SWR != null) {
				fixed (SwrContext** p1 = &m_pAudio_SWR) {
					ffmpeg.swr_free(p1);
				}
				m_pAudio_SWR = null;
			}

			m_buffer_audio_out?.ClearBuffer();
			m_buffer_audio_out = null;

			m_nAudioStream_ID = -1;
			m_bInitAudio = false;
		}

		static string GetTimeDuration(TimeSpan ts) {
			string strpos = "";
			if (ts.TotalHours > 9) {
				strpos = ((int)ts.TotalHours) + ts.ToString(@"\:mm\:ss");
			}
			else {
				strpos = ts.ToString(@"hh\:mm\:ss");
			}

			return strpos;
		}

		static void PlayAudio(string strfilepath) {
			
			int nret = 0;
			int i;
			bool m_bInitAudio = false;

			while (true) {
				AVFormatContext* avformatcontext = ffmpeg.avformat_alloc_context();
				nret = ffmpeg.avformat_open_input(&avformatcontext, strfilepath, null, null);
				if (nret != 0) {
					break;
				}
				m_avformatcontext = avformatcontext;

				nret = ffmpeg.avformat_find_stream_info(m_avformatcontext, null);
				if (nret != 0) {
					break;
				}

				// dump(print) media file info
				ffmpeg.av_dump_format(m_avformatcontext, 0, strfilepath, 0);
				m_dbl_duration = avformatcontext->duration;

				// Check whether Audio Stream exist
				for (i = 0; i < avformatcontext->nb_streams; i++) {
					if (avformatcontext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO) {
						m_nAudioStream_ID = i;
					}
				}

				if (m_nAudioStream_ID == -1) {
					Console.WriteLine("= Audio Stream not found");
					break;
				}

				// Find Audio Codec and setting for Audio Device
				if (m_nAudioStream_ID != -1) {
					m_pAudio_Codec = (AVCodec*)ffmpeg.avcodec_find_decoder(avformatcontext->streams[m_nAudioStream_ID]->codecpar->codec_id);
					if (m_pAudio_Codec != null) {
						m_pAudio_CodecPar = avformatcontext->streams[m_nAudioStream_ID]->codecpar;
						m_pAudio_CodecCtx = ffmpeg.avcodec_alloc_context3(m_pAudio_Codec);
						ffmpeg.avcodec_parameters_to_context(m_pAudio_CodecCtx, m_pAudio_CodecPar);

						if (ffmpeg.avcodec_open2(m_pAudio_CodecCtx, m_pAudio_Codec, null) >= 0) {
							m_bInitAudio = true;

							AVChannelLayout channelLayout;
							ffmpeg.av_channel_layout_default(&channelLayout, 2);
							m_audio_channel_layout = channelLayout;


							SwrContext* swr = null;
							nret = ffmpeg.swr_alloc_set_opts2(
								&swr,
								&channelLayout,
								AVSampleFormat.AV_SAMPLE_FMT_S16,
								48000,
								&m_pAudio_CodecCtx->ch_layout,
								m_pAudio_CodecCtx->sample_fmt,
								m_pAudio_CodecCtx->sample_rate,
								0,
								null
							);
							if (nret == 0) {
								m_pAudio_SWR = swr;

								nret = ffmpeg.swr_init(swr);
								if (nret == 0) {
									m_bInitAudio = true;
								}
							}
						}
					}
				}

				if (m_bInitAudio == false) {
					Console.WriteLine("= Failed to init Audio stream");
					break;
				}
				break;
			}

			if (m_bInitAudio == true) {
				AVPacket* pPacket = null;
				AVFrame* pFrame = null;

				pPacket = ffmpeg.av_packet_alloc();
				pFrame = ffmpeg.av_frame_alloc();
				byte[] managedBuffer = new byte[192000];
				byte** audio_outArr = stackalloc byte*[1];

				AVRational m_avrational_video_stream_time_base = m_avformatcontext->streams[m_nAudioStream_ID]->time_base;

				m_buffer_audio_out = new BufferedWaveProvider(m_audio_out_waveformat) {
					DiscardOnBufferOverflow = false
					//BufferLength = 1024 * 1024 * 5
				};

				m_audio_out = new WasapiOut(AudioClientShareMode.Shared, true, 20);
				m_audio_out.Init(m_buffer_audio_out);
				m_audio_out.Play();

				while (true) {
					nret = ffmpeg.av_read_frame(m_avformatcontext, pPacket);
					if (nret >= 0) {
						if (pPacket->stream_index == m_nAudioStream_ID) {

						}
						else {
							ffmpeg.av_packet_unref(pPacket);
							continue;
						}
					}
					else if (nret < 0) {
						if (nret == ffmpeg.AVERROR_EOF) {
							break;
						}
						continue;
					}

					// Request Stream
					ffmpeg.avcodec_send_packet(m_pAudio_CodecCtx, pPacket);

					while (true) {
						nret = ffmpeg.avcodec_receive_frame(m_pAudio_CodecCtx, pFrame);
						if (nret == ffmpeg.AVERROR_EOF) {
							break;
						}
						else if (nret == ffmpeg.AVERROR(ffmpeg.EAGAIN)) {
							break;
						}
						else if (nret < 0) {
							break;
						}

						// Do Parse Audio Stream
						double audioPts = pFrame->best_effort_timestamp * ffmpeg.av_q2d(m_avrational_video_stream_time_base);

						string strdecode_pos = GetTimeDuration(TimeSpan.FromSeconds((double)audioPts));

						TimeSpan tsplay = m_audio_out.GetPositionTimeSpan();
						Console.WriteLine($"[Decode Pos] = {strdecode_pos}, [Play Pos] = {GetTimeDuration(tsplay)}");

						// Check Samples data's oveflow
						while (m_buffer_audio_out.BufferedBytes > m_buffer_audio_out.BufferLength / 2) {
							System.Threading.Thread.Sleep(50);
							tsplay = m_audio_out.GetPositionTimeSpan();
							Console.WriteLine($"[Decode Pos] = {strdecode_pos}, [Play Pos] = {GetTimeDuration(tsplay)}");
						}

						int nsamples = 0;

						fixed (byte* outPtr = managedBuffer) {
							audio_outArr[0] = outPtr;

							nsamples = ffmpeg.swr_convert(
								m_pAudio_SWR,
								audio_outArr,
								pFrame->nb_samples,
								pFrame->extended_data,
								pFrame->nb_samples);

							if (nsamples > 0) {
								int bytes = nsamples * 2 * 2;
								m_buffer_audio_out.AddSamples(managedBuffer, 0, bytes);
							}
							else {
								break;
							}

							nsamples = ffmpeg.swr_convert(
								m_pAudio_SWR,
								audio_outArr,
								pFrame->nb_samples,
								null,
								0);

							if (nsamples > 0) {
								int bytes = nsamples * 2 * 2;
								m_buffer_audio_out.AddSamples(managedBuffer, 0, bytes);
							}
							else {
								break;
							}
						}

						ffmpeg.av_frame_unref(pFrame);
					}


					ffmpeg.av_packet_unref(pPacket);
				}

				// Play left audio buffer
				while (m_buffer_audio_out.BufferedBytes > 0) {
					Thread.Sleep(10);
				}
			}
		}
	}
}
