#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Pablo Ferraris
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion License

using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinkupSharp.Serializers
{
    public abstract class PacketSerializerBase : IPacketSerializer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PacketSerializerBase));
        private List<byte> buffer;

        public PacketSerializerBase()
        {
            buffer = new List<byte>();
        }

        public byte[] Serialize(Packet packet, byte[] token = null)
        {
            var result = Packet2Bytes(packet);
            if (token != null)
                result = result.Concat(token).ToArray();
            return result;
        }

        public List<Packet> Deserialize(byte[] bytes, byte[] token = null)
        {
            var packets = new List<Packet>();
            lock (buffer)
            {
                buffer.AddRange(bytes);
                while (ContainsPacket(token))
                {
                    try
                    {
                        byte[] packet = ReadPacket(token);
                        if (packet.Length > 0)
                            packets.Add(Bytes2Packet(packet));
                    }
                    catch (Exception ex)
                    {
                        log.Error("Deserialization error", ex);
                    }
                }
            }
            return packets;
        }

        private bool ContainsPacket(byte[] token)
        {
            if (buffer.Count == 0) return false;
            if (token == null) return true;
            int start = buffer.Count - token.Length;
            int pos;
            while ((pos = buffer.LastIndexOf(token.First(), start)) >= 0)
            {
                start = pos - 1;
                if (buffer.Skip(pos).SequenceEqual(token))
                    return true;
            }
            return false;
        }

        private byte[] ReadPacket(byte[] token)
        {
            if (token == null)
            {
                var bytes = buffer.ToArray();
                buffer.Clear();
                return bytes;
            }
            int pos;
            while ((pos = buffer.IndexOf(token.First())) >= 0)
            {
                if (buffer.Skip(pos).Take(token.Length).SequenceEqual(token))
                {
                    var bytes = buffer.Take(pos).ToArray();
                    buffer.RemoveRange(0, pos + token.Length);
                    return bytes;
                }
            }
            return new byte[0];
        }

        protected abstract Packet Bytes2Packet(byte[] packet);
        protected abstract byte[] Packet2Bytes(Packet packet);
    }
}
