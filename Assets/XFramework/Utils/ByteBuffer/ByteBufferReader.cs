using System;
using System.Text;

namespace XGame.Core
{
    public sealed class ByteBufferReader
    {
        private readonly byte[] _bytes;

        private int _index = 0;

        public ByteBufferReader(byte[] bytes)
        {
            _bytes = bytes;
        }

        public bool IsValid
        {
            get
            {
                if (_bytes == null || _bytes.Length == 0)
                {
                    return false;
                }
                return true;
            }
        }

        public int Length
        {
            get => _bytes.Length;
        }

        public byte ReadByte()
        {
            return _bytes[_index++];
        }

        public byte[] ReadBytes(int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(_bytes, _index, result, 0, length);
            _index += length;
            return result;
        }

        public bool ReadBool()
        {
            return ReadByte() == 1;
        }

        public short ReadInt16()
        {
            // 对于小端字节序和大端字节序需要不同的处理
            if (BitConverter.IsLittleEndian)
            {
                short result = (short)(_bytes[_index] | _bytes[_index + 1] << 8);
                _index += 2;
                return result;
            }
            else
            {
                short result = (short)(_bytes[_index] << 8 | _bytes[_index + 1]);
                _index += 2;
                return result;
            }
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        public int ReadInt32()
        {
            // 对于小端字节序和大端字节序需要不同的处理
            if (BitConverter.IsLittleEndian)
            {
                int result = _bytes[_index] | _bytes[_index + 1] << 8 | _bytes[_index + 2] << 16 | _bytes[_index + 3] << 24;
                _index += 4;
                return result;
            }
            else
            {
                int result = _bytes[_index] << 24 | _bytes[_index + 1] << 16 | _bytes[_index + 2] << 8 | _bytes[_index + 3];
                _index += 4;
                return result;
            }
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public long ReadInt64()
        {
            // 对于小端字节序和大端字节序需要不同的处理
            if (BitConverter.IsLittleEndian)
            {
                int i1 = (_bytes[_index]) | (_bytes[_index + 1] << 8) | (_bytes[_index + 2] << 16) | (_bytes[_index + 3] << 24);
                int i2 = (_bytes[_index + 4]) | (_bytes[_index + 5] << 8) | (_bytes[_index + 6] << 16) | (_bytes[_index + 7] << 24);
                _index += 8;
                return (uint)i1 | ((long)i2 << 32);
            }
            else
            {
                int i1 = (_bytes[_index] << 24) | (_bytes[_index + 1] << 16) | (_bytes[_index + 2] << 8) | (_bytes[_index + 3]);
                int i2 = (_bytes[_index + 4] << 24) | (_bytes[_index + 5] << 16) | (_bytes[_index + 6] << 8) | (_bytes[_index + 7]);
                _index += 8;
                return (uint)i2 | ((long)i1 << 32);
            }
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        public string ReadString(Encoding encoding)
        {
            ushort length = ReadUInt16();
            if (length == 0)
            {
                return string.Empty;
            }

            string result = encoding.GetString(_bytes, _index, length);
            _index += length;
            return result;
        }

        public string ReadUTF8String()
        {
            return ReadString(Encoding.UTF8);
        }

        public int[] ReadInt32Array()
        {
            ushort length = ReadUInt16();
            int[] result = new int[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadInt32();
            }
            return result;
        }

        public long[] ReadInt64Array()
        {
            ushort length = ReadUInt16();
            long[] result = new long[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadInt64();
            }
            return result;
        }

        public string[] ReadStringArray(Encoding encoding)
        {
            ushort length = ReadUInt16();
            string[] result = new string[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadString(encoding);
            }
            return result;
        }

        public string[] ReadUTF8StringArray()
        {
            return ReadStringArray(Encoding.UTF8);
        }
    }
}