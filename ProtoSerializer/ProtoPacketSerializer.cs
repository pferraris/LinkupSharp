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

using ProtoBuf;
using ProtoBuf.Meta;
using System.IO;

namespace LinkupSharp.Serializers
{
    public class ProtoPacketSerializer : PacketSerializerBase
    {
        static ProtoPacketSerializer()
        {
            var idDefinition = RuntimeTypeModel.Default.Add(typeof(Id), true);
            idDefinition.AddField(1, "Username");
            idDefinition.AddField(2, "Domain");
            var packetDefinition = RuntimeTypeModel.Default.Add(typeof(Packet), false);
            packetDefinition.AddField(1, "Sender");
            packetDefinition.AddField(2, "Recipient");
            packetDefinition.AddField(3, "Content");
            packetDefinition.AddField(4, "TypeName");
        }

        protected override Packet Bytes2Packet(byte[] packet)
        {
            return Serializer.Deserialize<Packet>(new MemoryStream(packet));
        }

        protected override byte[] Packet2Bytes(Packet packet)
        {
            MemoryStream stream = new MemoryStream();
            Serializer.Serialize(stream, packet);
            return stream.ToArray();
        }
    }
}
