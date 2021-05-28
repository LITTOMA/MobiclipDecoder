using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibMobiclip.Containers.Moflex;
using LibMobiclip.Codec;
using System.Drawing;
using AviFile;
using LibMobiclip.Codec.Mobiclip;
using LibMobiclip.Containers.Mods;
using LibMobiclip.Codec.Sx;
using LibMobiclip.Utils;
using LibMobiclip.Codec.FastAudio;
using CommandLine.Text;
using CommandLine;

namespace MobiConverter
{
    class Program
    {
        public class Options
        {
            [Option('d', "decode", SetName = "decode", HelpText = "Decode Mobiclip movie")]
            public bool Decode { get; set; }

            [Option('i', "input", Required = true, HelpText = "Input file")]
            public string Input { get; set; }

            [Option('o', "output", HelpText = "Output file.")]
            public string Output { get => Left; set => Left = value; }

            [Option('l', "left", HelpText = "Output left side video file name (Same as -o/--output)")]
            public string Left { get; set; }

            [Option('r', "right", HelpText = "Output right side video file name")]
            public string Right { get; set; }
        }

        static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult
              .WithNotParsed(errs => DisplayHelp(parserResult, errs))
              .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            if (options.Decode)
            {
                byte[] sig = new byte[4];
                Stream s = File.OpenRead(options.Input);
                s.Read(sig, 0, 4);
                s.Close();
                if (sig[0] == 0x4C && sig[1] == 0x32 && sig[2] == 0xAA && sig[3] == 0xAB)//moflex
                {
                    ConvertMoflex(options.Input, options.Output, options.Right);
                }
                else if (sig[0] == 0x4D && sig[1] == 0x4F && sig[2] == 0x44 && sig[3] == 0x53)
                {
                    ConvertMods(options.Input, options.Output);
                    return;
                }
                else if (sig[0] == 0x4D && sig[1] == 0x4F && sig[2] == 0x43 && sig[3] == 0x35)
                {
                    ConvertMoc5();
                    return;
                }
                else if (Path.GetExtension(options.Input).ToLower() == ".vx2")
                {
                    ConvertVx2(options.Input, options.Output);
                    return;
                }
                else
                {
                    Console.WriteLine("Error! Unrecognized format!");
                    return;
                }
            }
        }

        private static void DisplayHelp(ParserResult<Options> parserResult, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(parserResult, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e => e);
            Console.WriteLine(helpText);
        }



        //static void Main(string[] args)
        //{
        //    Console.WriteLine("MobiConverter by Gericom");
        //    Console.WriteLine();

        //    switch (args[0])
        //    {
        //        case "-d":
        //            {
        //                if (args.Length != 2 && args.Length != 3)
        //                    goto default;
        //                if (!File.Exists(args[1]))
        //                {
        //                    Console.WriteLine("Error! File not found: " + args[1]);
        //                    return;
        //                }
        //                string input = args[1];
        //                string outfile = (args.Length >= 3) ? args[2] : Path.ChangeExtension(input, "avi");
        //                string outRightFile = (args.Length == 4) ? args[3] : Path.GetFileNameWithoutExtension(input)+"_right.avi";


        //                byte[] sig = new byte[4];
        //                Stream s = File.OpenRead(input);
        //                s.Read(sig, 0, 4);
        //                s.Close();
        //                if (sig[0] == 0x4C && sig[1] == 0x32 && sig[2] == 0xAA && sig[3] == 0xAB)//moflex
        //                {
        //                    ConvertMoflex(input, outfile, outRightFile);
        //                }
        //                else if (sig[0] == 0x4D && sig[1] == 0x4F && sig[2] == 0x44 && sig[3] == 0x53)
        //                {
        //                    ConvertMods(input, outfile);
        //                    return;
        //                }
        //                else if (sig[0] == 0x4D && sig[1] == 0x4F && sig[2] == 0x43 && sig[3] == 0x35)
        //                {
        //                    ConvertMoc5();
        //                    return;
        //                }
        //                else if (Path.GetExtension(input).ToLower() == ".vx2")
        //                {
        //                    ConvertVx2(input, outfile);
        //                    return;
        //                }
        //                else
        //                {
        //                    Console.WriteLine("Error! Unrecognized format!");
        //                    return;
        //                }
        //                break;
        //            }
        //        case "-e":
        //            {

        //                break;
        //            }
        //        default:
        //        case "-h":
        //            return;
        //    }
        //}

        private static void ConvertMoc5()
        {
            //moc5
            Console.WriteLine("MOC5 container detected!");
            Console.WriteLine("Error! Not supported yet!");
            return;
        }

        private static void ConvertVx2(string input, string outfile)
        {
            //mods
            Console.WriteLine("VX2 container detected!");
            Console.Write("Converting: ");
            Console.CursorVisible = false;
            AviManager m = new AviManager(outfile, false);
            FileStream fs = File.OpenRead(input);
            MemoryStream audio = new MemoryStream();
            MobiclipDecoder d = new MobiclipDecoder(256, 192, MobiclipDecoder.MobiclipVersion.Moflex3DS);
            VideoStream vs = null;
            int framerate = 20;
            int counter = 0;
            int frame = 0;
            while (true)
            {
                if (fs.Position >= fs.Length) break;
                if ((frame % framerate) == 0)//Audio
                {
                    byte[] adata = new byte[32768 * 2];
                    fs.Read(adata, 0, 32768 * 2);
                    audio.Write(adata, 0, adata.Length);
                }
                int length = (fs.ReadByte() << 0) | (fs.ReadByte() << 8) | (fs.ReadByte() << 16) | (fs.ReadByte() << 24);
                byte[] data = new byte[length];
                fs.Read(data, 0, length);
                d.Data = data;
                d.Offset = 0;
                Bitmap b = d.DecodeFrame();
                if (vs == null) vs = m.AddVideoStream(false, framerate, b);
                else vs.AddFrame(b);
                frame++;
                //report progress
                if (counter == 0)
                {
                    Console.Write("{0,3:D}%", fs.Position * 100 / fs.Length);
                    Console.CursorLeft -= 4;
                }
                counter++;
                if (counter == 50) counter = 0;
            }
            if (audio != null)
            {
                byte[] adata = audio.ToArray();
                audio.Close();
                var sinfo = new Avi.AVISTREAMINFO();
                sinfo.fccType = Avi.streamtypeAUDIO;
                sinfo.dwScale = 1 * 2;
                sinfo.dwRate = (int)32768 * 1 * 2;
                sinfo.dwSampleSize = 1 * 2;
                sinfo.dwQuality = -1;
                var sinfo2 = new Avi.PCMWAVEFORMAT();
                sinfo2.wFormatTag = 1;
                sinfo2.nChannels = (short)1;
                sinfo2.nSamplesPerSec = (int)32768;
                sinfo2.nAvgBytesPerSec = (int)32768 * 1 * 2;
                sinfo2.nBlockAlign = (short)(1 * 2);
                sinfo2.wBitsPerSample = 16;
                unsafe
                {
                    fixed (byte* pAData = &adata[0])
                    {
                        m.AddAudioStream((IntPtr)pAData, sinfo, sinfo2, adata.Length);
                    }
                }
            }
            m.Close();
            fs.Close();
            Console.WriteLine("Done!");
            Console.CursorVisible = true;
            return;
        }

        private static void ConvertMods(string input, string outfile)
        {

            //mods
            Console.WriteLine("Mods container detected!");
            Console.Write("Converting: ");
            Console.CursorVisible = false;
            AviManager m = new AviManager(outfile, false);
            FileStream stream = File.OpenRead(input);
            ModsDemuxer dm = new ModsDemuxer(stream);
            MemoryStream audio = null;
            if ((dm.Header.AudioCodec == 1 || dm.Header.AudioCodec == 3) && dm.Header.NbChannel > 0 && dm.Header.Frequency > 0)
            {
                audio = new MemoryStream();
            }
            MobiclipDecoder d = new MobiclipDecoder(dm.Header.Width, dm.Header.Height, MobiclipDecoder.MobiclipVersion.ModsDS);
            VideoStream vs = null;
            int CurChannel = 0;
            List<short>[] channels = new List<short>[dm.Header.NbChannel];
            IMAADPCMDecoder[] decoders = new IMAADPCMDecoder[dm.Header.NbChannel];
            SxDecoder[] sxd = new SxDecoder[dm.Header.NbChannel];
            FastAudioDecoder[] fad = new FastAudioDecoder[dm.Header.NbChannel];
            bool[] isinit = new bool[dm.Header.NbChannel];
            for (int i = 0; i < dm.Header.NbChannel; i++)
            {
                channels[i] = new List<short>();
                decoders[i] = new IMAADPCMDecoder();
                sxd[i] = new SxDecoder();
                fad[i] = new FastAudioDecoder();
                isinit[i] = false;
            }
            int counter = 0;
            while (true)
            {
                uint NrAudioPackets;
                bool IsKeyFrame;
                byte[] framedata = dm.ReadFrame(out NrAudioPackets, out IsKeyFrame);
                if (framedata == null) break;
                d.Data = framedata;
                d.Offset = 0;
                Bitmap b = d.DecodeFrame();
                if (vs == null) vs = m.AddVideoStream(false, Math.Round(dm.Header.Fps / (double)0x01000000, 3), b);
                else vs.AddFrame(b);
                if (NrAudioPackets > 0 && audio != null)
                {
                    int Offset = d.Offset - 2;
                    if (dm.Header.TagId == 0x334E && (IOUtil.ReadU16LE(framedata, 0) & 0x8000) != 0)
                        Offset += 4;
                    if (dm.Header.AudioCodec == 3)
                    {
                        if (IsKeyFrame)
                        {
                            for (int i = 0; i < dm.Header.NbChannel; i++)
                            {
                                channels[i] = new List<short>();
                                decoders[i] = new IMAADPCMDecoder();
                                sxd[i] = new SxDecoder();
                                fad[i] = new FastAudioDecoder();
                                isinit[i] = false;
                            }
                        }
                        for (int i = 0; i < NrAudioPackets; i++)
                        {
                            channels[CurChannel].AddRange(decoders[CurChannel].GetWaveData(framedata, Offset, 128 + (!isinit[CurChannel] ? 4 : 0)));
                            Offset += 128 + (!isinit[CurChannel] ? 4 : 0);
                            isinit[CurChannel] = true;
                            CurChannel++;
                            if (CurChannel >= dm.Header.NbChannel) CurChannel = 0;
                        }
                    }
                    else if (dm.Header.AudioCodec == 1)
                    {
                        for (int i = 0; i < NrAudioPackets; i++)
                        {
                            if (!isinit[CurChannel]) sxd[CurChannel].Codebook = dm.AudioCodebooks[CurChannel];
                            isinit[CurChannel] = true;
                            sxd[CurChannel].Data = framedata;
                            sxd[CurChannel].Offset = Offset;
                            channels[CurChannel].AddRange(sxd[CurChannel].Decode());
                            Offset = sxd[CurChannel].Offset;
                            CurChannel++;
                            if (CurChannel >= dm.Header.NbChannel) CurChannel = 0;
                        }
                    }
                    else if (dm.Header.AudioCodec == 2)
                    {
                        for (int i = 0; i < NrAudioPackets; i++)
                        {
                            fad[CurChannel].Data = framedata;
                            fad[CurChannel].Offset = Offset;
                            channels[CurChannel].AddRange(fad[CurChannel].Decode());
                            Offset = fad[CurChannel].Offset;
                            CurChannel++;
                            if (CurChannel >= dm.Header.NbChannel) CurChannel = 0;
                        }
                    }
                    int smallest = int.MaxValue;
                    for (int i = 0; i < dm.Header.NbChannel; i++)
                    {
                        if (channels[i].Count < smallest) smallest = channels[i].Count;
                    }
                    if (smallest > 0)
                    {
                        //Gather samples
                        short[][] samps = new short[dm.Header.NbChannel][];
                        for (int i = 0; i < dm.Header.NbChannel; i++)
                        {
                            samps[i] = new short[smallest];
                            channels[i].CopyTo(0, samps[i], 0, smallest);
                            channels[i].RemoveRange(0, smallest);
                        }
                        byte[] result = InterleaveChannels(samps);
                        audio.Write(result, 0, result.Length);
                    }
                }
                //report progress
                if (counter == 0)
                {
                    Console.Write("{0,3:D}%", stream.Position * 100 / stream.Length);
                    Console.CursorLeft -= 4;
                }
                counter++;
                if (counter == 50) counter = 0;
            }
            if (audio != null)
            {
                byte[] adata = audio.ToArray();
                audio.Close();
                var sinfo = new Avi.AVISTREAMINFO();
                sinfo.fccType = Avi.streamtypeAUDIO;
                sinfo.dwScale = dm.Header.NbChannel * 2;
                sinfo.dwRate = (int)dm.Header.Frequency * dm.Header.NbChannel * 2;
                sinfo.dwSampleSize = dm.Header.NbChannel * 2;
                sinfo.dwQuality = -1;
                var sinfo2 = new Avi.PCMWAVEFORMAT();
                sinfo2.wFormatTag = 1;
                sinfo2.nChannels = (short)dm.Header.NbChannel;
                sinfo2.nSamplesPerSec = (int)dm.Header.Frequency;
                sinfo2.nAvgBytesPerSec = (int)dm.Header.Frequency * dm.Header.NbChannel * 2;
                sinfo2.nBlockAlign = (short)(dm.Header.NbChannel * 2);
                sinfo2.wBitsPerSample = 16;
                unsafe
                {
                    fixed (byte* pAData = &adata[0])
                    {
                        m.AddAudioStream((IntPtr)pAData, sinfo, sinfo2, adata.Length);
                    }
                }
            }
            m.Close();
            stream.Close();
            Console.WriteLine("Done!");
            Console.CursorVisible = true;
            return;
        }

        private static void ConvertMoflex(string input, string leftFile, string rightFile)
        {
            Console.WriteLine("Moflex container detected!");
            Console.Write("Converting: ");
            Console.CursorVisible = false;
            MobiclipDecoder mobiCodec = null;

            AviManager leftAviManager = string.IsNullOrEmpty(leftFile) ? null : new AviManager(leftFile, false);
            AviManager rightAviManager = string.IsNullOrEmpty(rightFile) ? null : new AviManager(rightFile, false);

            MemoryStream audio = null;
            FastAudioDecoder[] mFastAudioDecoders = null;
            int audiorate = -1;
            int audiochannels = 0;

            VideoStream leftVideoStream = null;
            VideoStream rightVideoStream = null;

            FileStream inputStream = File.OpenRead(input);
            var liveDemux = new MoLiveDemux(inputStream);
            int PlayingVideoStream = -1;

            bool is3dVideo = false;
            bool isLeftFrame = false;

            liveDemux.OnCompleteFrameReceived += delegate (MoLiveChunk Chunk, byte[] Data)
            {
                if ((Chunk is MoLiveStreamVideo || Chunk is MoLiveStreamVideoWithLayout) && ((PlayingVideoStream == -1) || ((MoLiveStream)Chunk).StreamIndex == PlayingVideoStream))
                {
                    isLeftFrame = !isLeftFrame;
                    if (mobiCodec == null)
                    {
                        mobiCodec = new MobiclipDecoder(((MoLiveStreamVideo)Chunk).Width, ((MoLiveStreamVideo)Chunk).Height, MobiclipDecoder.MobiclipVersion.Moflex3DS);
                        PlayingVideoStream = ((MoLiveStream)Chunk).StreamIndex;
                        if (!(Chunk is MoLiveStreamVideoWithLayout)) is3dVideo = false;
                        else if (((MoLiveStreamVideoWithLayout)Chunk).ImageLayout == MoLiveStreamVideoWithLayout.VideoLayout.Simple2D) is3dVideo = false;
                        else is3dVideo = true;
                    }
                    mobiCodec.Data = Data;
                    mobiCodec.Offset = 0;
                    Bitmap b = mobiCodec.DecodeFrame();
                    if ((!is3dVideo || isLeftFrame) && leftAviManager != null)
                    {
                        if (leftVideoStream == null) leftVideoStream = leftAviManager.AddVideoStream(false, Math.Round(((double)((MoLiveStreamVideo)Chunk).FpsRate) / ((double)((MoLiveStreamVideo)Chunk).FpsScale), 3), b);
                        else leftVideoStream.AddFrame(b);
                    }
                    else if (is3dVideo && !isLeftFrame && rightAviManager != null)
                    {
                        if (rightVideoStream == null) rightVideoStream = rightAviManager.AddVideoStream(false, Math.Round(((double)((MoLiveStreamVideo)Chunk).FpsRate) / ((double)((MoLiveStreamVideo)Chunk).FpsScale), 3), b);
                        else rightVideoStream.AddFrame(b);
                    }

                }
                else if (Chunk is MoLiveStreamAudio moAudio)
                {
                    if (audio == null)
                    {
                        audio = new MemoryStream();
                        audiochannels = (int)moAudio.Channel;
                        audiorate = (int)moAudio.Frequency;
                    }
                    switch ((int)moAudio.CodecId)
                    {
                        case 0://fastaudio
                            {
                                if (mFastAudioDecoders == null)
                                {
                                    mFastAudioDecoders = new FastAudioDecoder[(int)moAudio.Channel];
                                    for (int i = 0; i < (int)moAudio.Channel; i++)
                                    {
                                        mFastAudioDecoders[i] = new FastAudioDecoder();
                                    }
                                }
                                List<short>[] channels = new List<short>[(int)moAudio.Channel];
                                for (int i = 0; i < (int)moAudio.Channel; i++)
                                {
                                    channels[i] = new List<short>();
                                }

                                int offset = 0;
                                int size = 40;
                                while (offset + size < Data.Length)
                                {
                                    for (int i = 0; i < (int)moAudio.Channel; i++)
                                    {
                                        mFastAudioDecoders[i].Data = Data;
                                        mFastAudioDecoders[i].Offset = offset;
                                        channels[i].AddRange(mFastAudioDecoders[i].Decode());
                                        offset = mFastAudioDecoders[i].Offset;
                                    }
                                }
                                short[][] channelsresult = new short[(int)moAudio.Channel][];
                                for (int i = 0; i < (int)moAudio.Channel; i++)
                                {
                                    channelsresult[i] = channels[i].ToArray();
                                }
                                byte[] result = InterleaveChannels(channelsresult);
                                audio.Write(result, 0, result.Length);
                            }
                            break;
                        case 1://IMA-ADPCM
                            {
                                IMAADPCMDecoder[] decoders = new IMAADPCMDecoder[(int)moAudio.Channel];
                                List<short>[] channels = new List<short>[(int)moAudio.Channel];
                                for (int i = 0; i < (int)moAudio.Channel; i++)
                                {
                                    decoders[i] = new IMAADPCMDecoder();
                                    decoders[i].GetWaveData(Data, 4 * i, 4);
                                    channels[i] = new List<short>();
                                }

                                int offset = 4 * (int)moAudio.Channel;
                                int size = 128 * (int)moAudio.Channel;
                                while (offset + size < Data.Length)
                                {
                                    for (int i = 0; i < (int)moAudio.Channel; i++)
                                    {
                                        channels[i].AddRange(decoders[i].GetWaveData(Data, offset, 128));
                                        offset += 128;
                                    }
                                }
                                short[][] channelsresult = new short[(int)moAudio.Channel][];
                                for (int i = 0; i < (int)moAudio.Channel; i++)
                                {
                                    channelsresult[i] = channels[i].ToArray();
                                }
                                byte[] result = InterleaveChannels(channelsresult);
                                audio.Write(result, 0, result.Length);
                            }
                            break;
                        case 2://PCM16
                            {
                                audio.Write(Data, 0, Data.Length - (Data.Length % ((int)moAudio.Channel * 2)));
                            }
                            break;
                    }
                }
            };
            int counter = 0;
            while (true)
            {
                uint error = liveDemux.ReadPacket();
                if (error == 73)
                    break;
                //report progress
                if (counter == 0)
                {
                    Console.Write("{0,3:D}%", inputStream.Position * 100 / inputStream.Length);
                    Console.CursorLeft -= 4;
                }
                counter++;
                if (counter == 50) counter = 0;
            }
            if (audio != null)
            {
                byte[] adata = audio.ToArray();
                audio.Close();
                var sinfo = new Avi.AVISTREAMINFO();
                sinfo.fccType = Avi.streamtypeAUDIO;
                sinfo.dwScale = audiochannels * 2;
                sinfo.dwRate = audiorate * audiochannels * 2;
                sinfo.dwSampleSize = audiochannels * 2;
                sinfo.dwQuality = -1;
                var sinfo2 = new Avi.PCMWAVEFORMAT();
                sinfo2.wFormatTag = 1;
                sinfo2.nChannels = (short)audiochannels;
                sinfo2.nSamplesPerSec = audiorate;
                sinfo2.nAvgBytesPerSec = audiorate * audiochannels * 2;
                sinfo2.nBlockAlign = (short)(audiochannels * 2);
                sinfo2.wBitsPerSample = 16;
                unsafe
                {
                    fixed (byte* pAData = &adata[0])
                    {
                        leftAviManager?.AddAudioStream((IntPtr)pAData, sinfo, sinfo2, adata.Length);
                        //rightAviManager.AddAudioStream((IntPtr)pAData, sinfo, sinfo2, adata.Length);
                    }
                }
            }

            leftAviManager?.Close();
            rightAviManager?.Close();
            inputStream.Close();
            Console.WriteLine("Done!");
            Console.CursorVisible = true;
        }

        private static byte[] InterleaveChannels(params Int16[][] Channels)
        {
            if (Channels.Length == 0) return new byte[0];
            byte[] Result = new byte[Channels[0].Length * Channels.Length * 2];
            for (int i = 0; i < Channels[0].Length; i++)
            {
                for (int j = 0; j < Channels.Length; j++)
                {
                    Result[i * 2 * Channels.Length + j * 2] = (byte)(Channels[j][i] & 0xFF);
                    Result[i * 2 * Channels.Length + j * 2 + 1] = (byte)(Channels[j][i] >> 8);
                }
            }
            return Result;
        }
    }
}
