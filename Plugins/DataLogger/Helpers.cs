﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LynLogger
{
    public static class Helpers
    {
        public static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        public static long UnixTimestamp { get { return ToUnixTimestamp(DateTimeOffset.UtcNow); } }

        public static long ToUnixTimestamp(DateTimeOffset dt)
        {
            return (long)dt.Subtract(Epoch).TotalSeconds;
        }

        public static DateTimeOffset FromUnixTimestamp(long ts)
        {
            return Epoch.AddSeconds(ts);
        }

        private const string Base32Charset = "0123456789ABCDEFGHJKLMNPQRSTVWXYZ";
        public static string Base32Encode(this byte[] buf)
        {
            ushort tmp = 0;
            int bits = 0;
            StringBuilder s = new StringBuilder(buf.Length * 8 / 5);
            for(int i = 0; i < buf.Length; i++) {
                tmp <<= 8;
                tmp |= buf[i];
                bits += 8;
                while(bits >= 5) {
                    s.Append(Base32Charset[tmp >> (bits - 5)]);
                    bits -= 5;
                    tmp <<= 16-bits;
                    tmp &= 0xFE00;
                    tmp >>= 16-bits;
                }
            }
            if(bits != 0) {
                s.Append(Base32Charset[tmp << (5 - bits)]);
                for(int i = 0; i < 5-bits; i++) {
                    s.Append(Base32Charset[32]);
                }
            }
            return s.ToString();
        }

        public static void WriteVLCI(this Stream output, ulong val)
        {
            for(int i = 0; i < 9; i++) {
                byte thisByte = (byte)(val & 0x7F);
                val >>= 7;
                if(val != 0) {
                    thisByte |= 0x80;
                    output.WriteByte(thisByte);
                } else {
                    output.WriteByte(thisByte);
                    break;
                }
            }
        }

        public static ulong ReadVLCI(this Stream input)
        {
            ulong val = 0;
            for(int i = 0; i < 9; i++) {
                int b = input.ReadByte();
                if(b < 0)
                    throw new EndOfStreamException();

                ulong l = (ulong)(b & 0x7F);
                val |= l << (7*i);

                if((b & 0x80) == 0) break;
                if(i == 8) {
                    if((b & 0x80) != 0) val |= 1UL << 63;
                }
            }

            return val;
        }

        private static readonly byte[] _blankByteArray = new byte[0];
        private const int maxDictLogSize = 24;
        public static void CompressData(Stream input, byte[] training, Stream output, int lp = 0, int lc = 3, int pb = 2, int fb = 32, string mf = "BT4")
        {
            if(training == null) training = _blankByteArray;
            using(MemoryStream train = new MemoryStream(training, false)) {
                using(SevenZip.DoubleStream stream = new SevenZip.DoubleStream()) {
                    stream.s1 = train;
                    stream.s2 = input;
                    stream.fileIndex = 0;
                    stream.skipSize = 0;

                    int dictLogSize;
                    bool eos;
                    long dataLen;
                    if(input.CanSeek) {
                        dataLen = input.Length;
                        var totalLen = training.Length + dataLen;
                        for(dictLogSize = 10; dictLogSize < maxDictLogSize; dictLogSize++) {
                            if(totalLen < (1 << dictLogSize)) break;
                        }
                        eos = false;
                    } else {
                        dataLen = -1;
                        dictLogSize = maxDictLogSize;
                        eos = true;
                    }
                    int dictSize = 1 << dictLogSize;

                    if(train.Length > dictSize) stream.skipSize = train.Length - dictSize;
                    train.Seek(stream.skipSize, SeekOrigin.Begin);

                    SevenZip.Compression.LZMA.Encoder comp = new SevenZip.Compression.LZMA.Encoder();
                    comp.SetCoderProperties(new SevenZip.CoderPropID[] {
                        SevenZip.CoderPropID.LitContextBits,
                        SevenZip.CoderPropID.LitPosBits,
                        SevenZip.CoderPropID.PosStateBits,
                        SevenZip.CoderPropID.DictionarySize,
                        SevenZip.CoderPropID.EndMarker,
                        SevenZip.CoderPropID.MatchFinder,
                        SevenZip.CoderPropID.NumFastBytes,
                        SevenZip.CoderPropID.Algorithm
                    }, new object[] {
                        lc,
                        lp,
                        pb,
                        dictSize,
                        eos,
                        mf,
                        fb,
                        2
                    });
                    comp.SetTrainSize((uint)(train.Length - stream.skipSize));

                    output.WriteByte((byte)((pb * 5 + lp) * 9 + lc));
                    output.WriteByte((byte)dictLogSize);
                    output.WriteVLCI((ulong)dataLen);

                    comp.Code(stream, output, -1, -1, null);
                }
            }
        }

        public static void DecompressData(Stream input, byte[] training, Stream output)
        {
            byte[] reconsProp = new byte[5];
            int conf = input.ReadByte();
            int dictLogSize = input.ReadByte();
            if((conf | dictLogSize) < 0)
                throw new EndOfStreamException();

            if(dictLogSize > maxDictLogSize)
                throw new InvalidDataException();

            ulong len = input.ReadVLCI();

            reconsProp[0] = (byte)conf;
            uint dictSize = 1U << dictLogSize;
            for(int i = 0; i < 4; i++) {
                reconsProp[i+1] = (byte)(dictSize >> (i*8));
            }

            long compSize = input.Length - input.Position;

            SevenZip.Compression.LZMA.Decoder dec = new SevenZip.Compression.LZMA.Decoder();
            dec.SetDecoderProperties(reconsProp);
            if(training != null) {
                using(MemoryStream ms = new MemoryStream(training, false)) {
                    dec.Train(ms);
                }
            }

            dec.Code(input, output, compSize, (long)len, null);
        }

        public static void Terminate(this object dummy) { }

        public static IEnumerable<TResult> SelectWithPrevious<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> projection)
        {
            using(var enumerator = source.GetEnumerator()) {
                if(!enumerator.MoveNext()) yield break;
                var previous = enumerator.Current;
                while(enumerator.MoveNext()) {
                    yield return projection(previous, enumerator.Current);
                    previous = enumerator.Current;
                }
            }
        }

        public static string LookupShipName(int id)
        {
            var ship = Grabacr07.KanColleWrapper.KanColleClient.Current.Master.Ships[id];
            if(ship == null) {
                return "Ship" + id;
            }
            return ship.Name;
        }

        public static string LookupShipTypeName(int id)
        {
            var ship = Grabacr07.KanColleWrapper.KanColleClient.Current.Master.Ships[id];
            if(ship == null) {
                return "Type"+id;
            }
            return ship.ShipType.Name;
        }

        public static int LookupShipTypeId(int id)
        {
            var ship = Grabacr07.KanColleWrapper.KanColleClient.Current.Master.Ships[id];
            if(ship == null) {
                return 0;
            }
            return ship.ShipType.Id;
        }

        /*public static string LookupTypeName(int id)
        {
            var type = Grabacr07.KanColleWrapper.KanColleClient.Current.Master.ShipTypes[id];
            if(type == null) {
                return "Type" + id;
            }
            return type.Name;
        }*/
    }
}