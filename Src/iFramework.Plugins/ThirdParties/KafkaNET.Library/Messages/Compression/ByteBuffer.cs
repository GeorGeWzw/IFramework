﻿using System;
using System.IO;

namespace Kafka.Client.Messages.Compression
{
    public struct ByteBuffer
    {
        private uint offset;

        public static ByteBuffer NewAsync(byte[] buffer)
        {
            return new ByteBuffer(buffer, 0, buffer.Length);
        }

        public static ByteBuffer NewAsync(byte[] buffer, int offset, int length)
        {
            return new ByteBuffer(buffer, (uint) offset, length);
        }

        public static ByteBuffer NewSync(byte[] buffer)
        {
            return new ByteBuffer(buffer, 0x80000000u, buffer.Length);
        }

        public static ByteBuffer NewSync(byte[] buffer, int offset, int length)
        {
            return new ByteBuffer(buffer, (uint) offset | 0x80000000u, length);
        }

        public static ByteBuffer NewEmpty()
        {
            return new ByteBuffer(BitArrayManipulation.EmptyByteArray, 0, 0);
        }

        private ByteBuffer(byte[] buffer, uint offset, int length)
        {
            Buffer = buffer;
            this.offset = offset;
            Length = length;
        }

        public byte[] Buffer { get; private set; }

        public int Offset => (int) (offset & 0x7fffffffu);
        public int Length { get; }

        public bool AsyncSafe => (offset & 0x80000000u) == 0u;

        public ByteBuffer ToAsyncSafe()
        {
            if (AsyncSafe)
            {
                return this;
            }
            var copy = new byte[Length];
            Array.Copy(Buffer, Offset, copy, 0, Length);
            return NewAsync(copy);
        }

        public void MakeAsyncSafe()
        {
            if (AsyncSafe)
            {
                return;
            }
            var copy = new byte[Length];
            Array.Copy(Buffer, Offset, copy, 0, Length);
            Buffer = copy;
            offset = 0;
        }

        public ByteBuffer ResizingAppend(ByteBuffer append)
        {
            if (AsyncSafe)
            {
                if (Offset + Length + append.Length <= Buffer.Length)
                {
                    Array.Copy(append.Buffer, append.Offset, Buffer, Offset + Length, append.Length);
                    return NewAsync(Buffer, Offset, Length + append.Length);
                }
            }
            var newCapacity = Math.Max(Length + append.Length, Length * 2);
            var newBuffer = new byte[newCapacity];
            Array.Copy(Buffer, Offset, newBuffer, 0, Length);
            Array.Copy(append.Buffer, append.Offset, newBuffer, Length, append.Length);
            return NewAsync(newBuffer, 0, Length + append.Length);
        }

        internal ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(Buffer, Offset, Length);
        }

        internal byte[] ToByteArray()
        {
            var safeSelf = ToAsyncSafe();
            var buf = safeSelf.Buffer ?? BitArrayManipulation.EmptyByteArray;
            if (safeSelf.Offset == 0 && safeSelf.Length == buf.Length)
            {
                return buf;
            }
            var copy = new byte[safeSelf.Length];
            Array.Copy(safeSelf.Buffer, safeSelf.Offset, copy, 0, safeSelf.Length);
            return copy;
        }

        internal MemoryStream ToStream()
        {
            return new MemoryStream(ToByteArray());
        }
    }
}