using System;
using System.Text;

namespace XGame.Core
{
    public sealed class ByteBufferWriter
    {
        private readonly byte[] _bytes;
        private int _index = 0;

        public ByteBufferWriter(int size)
        {
            _bytes = new byte[size];
        }

        public byte[] Bytes
        {
            get => _bytes;
        }

        public int Size
        {
            get => _bytes.Length;
        }

        public void Clear()
        {
            _index = 0;
        }

        public void WriteByte(byte value)
        {
            _bytes[_index++] = value;
        }

        public void WriteBytes(byte[] values)
        {
            int length = values.Length;
            for (int i = 0; i < length; i++)
            {
                _bytes[_index++] = values[i];
            }
        }

        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        public void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        public void WriteUInt16(ushort value)
        {
            _bytes[_index++] = (byte)value;
            _bytes[_index++] = (byte)(value >> 8);
        }

        public void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }

        public void WriteUInt32(uint value)
        {
            _bytes[_index++] = (byte)value;
            _bytes[_index++] = (byte)(value >> 8);
            _bytes[_index++] = (byte)(value >> 16);
            _bytes[_index++] = (byte)(value >> 24);
        }

        public void WriteInt64(long value)
        {
            WriteUInt64((ulong)value);
        }

        public void WriteUInt64(ulong value)
        {
            _bytes[_index++] = (byte)value;
            _bytes[_index++] = (byte)(value >> 8);
            _bytes[_index++] = (byte)(value >> 16);
            _bytes[_index++] = (byte)(value >> 24);
            _bytes[_index++] = (byte)(value >> 32);
            _bytes[_index++] = (byte)(value >> 40);
            _bytes[_index++] = (byte)(value >> 48);
            _bytes[_index++] = (byte)(value >> 56);
        }

        public void WriteString(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteInt16(0);
                return;
            }
            byte[] bytes = encoding.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new FormatException($"WriteString failed. String is too long, the length cannot exceed {ushort.MaxValue} bytes.");
            }
            WriteUInt16(Convert.ToUInt16(bytes.Length));
            WriteBytes(bytes);
        }

        public void WriteUTF8String(string value)
        {
            WriteString(value, Encoding.UTF8);
        }

        public void WriteInt32Array(int[] values)
        {
            if (values == null)
            {
                WriteUInt16(0);
                return;
            }
            if (values.Length > ushort.MaxValue)
            {
                throw new FormatException($"WriteInt32Array failed. Array length is too long, the length cannot exceed {ushort.MaxValue}.");
            }
            WriteUInt16(Convert.ToUInt16(values.Length));
            for (int i = 0; i < values.Length; i++)
            {
                WriteInt32(values[i]);
            }
        }

        public void WriteInt64Array(long[] values)
        {
            if (values == null)
            {
                WriteUInt16(0);
                return;
            }
            if (values.Length > ushort.MaxValue)
            {
                throw new FormatException($"WriteInt64Array failed. Array length is too long, the length cannot exceed {ushort.MaxValue}.");
            }
            WriteUInt16(Convert.ToUInt16(values.Length));
            for (int i = 0; i < values.Length; i++)
            {
                WriteInt64(values[i]);
            }
        }

        public void WriteStringArray(string[] values, Encoding encoding)
        {
            if (values == null)
            {
                WriteUInt16(0);
                return;
            }
            if (values.Length > ushort.MaxValue)
            {
                throw new FormatException($"WriteStringArray failed. Array length is too long, the length cannot exceed {ushort.MaxValue}.");
            }
            WriteUInt16(Convert.ToUInt16(values.Length));
            for (int i = 0; i < values.Length; i++)
            {
                WriteString(values[i], encoding);
            }
        }

        public void WriteUTF8StringArray(string[] values)
        {
            WriteStringArray(values, Encoding.UTF8);
        }
    }
}