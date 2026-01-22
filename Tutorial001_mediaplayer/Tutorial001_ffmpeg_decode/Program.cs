//////////////////////////////////////////////////////////////////////////////////////////////////
//	Projects		: Tutorial for mediaplay
//	Author			: 경덕현(cyberkdh@hotmail.com, cyberkdh@gmail.com)
//	Module			: DHApp.common
//	History			: 
//	Copyrights		: Copyright ⓒCYBERKDH. All Rights Reserved.
//////////////////////////////////////////////////////////////////////////////////////////////////

//
// Decode Audio from BigBuckBunny.mp4

using FFmpeg.AutoGen;

namespace Tutorial001_ffmpeg_decode{
    internal unsafe class Program{
        static void Main(string[] args){
            Console.WriteLine("Decode Start: BigBuckBunny.mp4");

			// Set ffmpeg DLL Path
			ffmpeg.RootPath = Path.Combine(GetPGMBasePath(), "ffmpeg_x64");
			DecodeAudio(Path.Combine(GetPGMBasePath(), "BigBuckBunny.mp4"));

			Console.WriteLine("Decode End: BigBuckBunny.mp4");
		}

		public static string GetPGMBasePath() {
			// return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			return System.IO.Path.GetDirectoryName(Environment.ProcessPath);

		}

		// start decode routine
		static void DecodeAudio(string strfilepath) {
			AVFormatContext* m_avformatcontext = null;
			int m_nAudioStream_ID = -1;
			AVCodec* m_pAudio_Codec = null;
			AVCodecParameters* m_pAudio_CodecPar = null;
			AVCodecContext* m_pAudio_CodecCtx = null;
			SwrContext* m_pAudio_SWR = null;
			AVChannelLayout m_audio_channel_layout = new AVChannelLayout();
			long m_dbl_duration = 0;
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

				if(m_nAudioStream_ID == -1) {
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

				if(m_bInitAudio == false) {
					Console.WriteLine("= Failed to init Audio stream");
					break;
				}
				break;
			}

			if(m_bInitAudio == true) {
				AVPacket* pPacket = null;
				AVFrame* pFrame = null;

				pPacket = ffmpeg.av_packet_alloc();
				pFrame = ffmpeg.av_frame_alloc();
				byte[] managedBuffer = new byte[192000];
				byte** audio_outArr = stackalloc byte*[1];

				AVRational m_avrational_video_stream_time_base = m_avformatcontext->streams[m_nAudioStream_ID]->time_base;

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
						if(nret == ffmpeg.AVERROR_EOF) {
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
						var ts = TimeSpan.FromSeconds((double)audioPts);
						string strpos = "";
						if (ts.TotalHours > 9) {
							strpos = ((int)ts.TotalHours) + ts.ToString(@"\:mm\:ss");
						}
						else {
							strpos = ts.ToString(@"hh\:mm\:ss");
						}
						Console.WriteLine($"[Cur Pos] ={strpos}");

						int nsamples = 0;

						fixed (byte* outPtr = managedBuffer) {
							audio_outArr[0] = outPtr;

							// Send Data To Sound Device
						}

						ffmpeg.av_frame_unref(pFrame);
					}
					

					ffmpeg.av_packet_unref(pPacket);
				}
			}


			// Free
			if (m_pAudio_CodecCtx != null) {
				ffmpeg.avcodec_free_context(&m_pAudio_CodecCtx);
				m_pAudio_CodecCtx = null;
			}

			if (m_avformatcontext != null) {
				ffmpeg.avformat_close_input(&m_avformatcontext);
				ffmpeg.avformat_free_context(m_avformatcontext);
				m_avformatcontext = null;
			}
			
			if (m_pAudio_SWR != null) {
				ffmpeg.swr_free(&m_pAudio_SWR);
				m_pAudio_SWR = null;
			}

			m_nAudioStream_ID = -1;
			m_bInitAudio = false;

		}
    }
}
