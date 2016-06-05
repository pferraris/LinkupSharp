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
    public class TokenizedPacketSerializer<T> : IPacketSerializer where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TokenizedPacketSerializer<T>));
        private T internalSerializer;
        private List<byte> buffer;
        private byte[] token;

        public TokenizedPacketSerializer(byte[] token)
        {
            this.token = token;
            internalSerializer = new T();
            buffer = new List<byte>();
        }

        public byte[] Serialize(Packet packet)
        {
            var result = internalSerializer.Serialize(packet);
            if (token != null)
                result = result.Concat(token).ToArray();
            return result;
        }

        public Packet Deserialize(byte[] bytes)
        {
            lock (buffer)
            {
                if (bytes != null)
                    buffer.AddRange(bytes);
                if (ContainsPacket)
                {
                    try
                    {
                        return internalSerializer.Deserialize(ReadPacket());
                    }
                    catch (Exception ex)
                    {
                        log.Error("Cannot deserialize tokenized packet", ex);
                    }
                }
            }
            return null;
        }

        private bool ContainsPacket
        {
            get
            {
                if (token == null)
                {
                    if (buffer.Count > 0)
                        return true;
                    else
                        return false;
                }
                if (buffer.Count <= token.Length) return false;
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
        }

        private byte[] ReadPacket()
        {
            if (token == null)
            {
                var bytes = buffer.ToArray();
                buffer.Clear();
                return bytes;
            }
            int pos = 0;
            while ((pos = buffer.IndexOf(token.First(), pos)) >= 0)
            {
                if (buffer.Skip(pos).Take(token.Length).SequenceEqual(token))
                {
                    var bytes = buffer.Take(pos).ToArray();
                    buffer.RemoveRange(0, pos + token.Length);
                    return bytes;
                }
                else
                    pos++;
            }
            return new byte[0];
        }
    }
}
