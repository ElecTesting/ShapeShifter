using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBaseReader
{
    public class BinReader : BinaryReader
    {
        public BinReader(Stream stream) : base(stream) { }

        public long Position
        {
            get { return BaseStream.Position; }
        }

        public long Length
        {
            get { return BaseStream.Length; }
        }

        public int ReadInt32BE()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public Int16 ReadInt16BE()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public UInt16 ReadUInt16BE()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public Int64 ReadInt64BE()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public double ReadDoubleBE()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }

        public UInt32 ReadUInt32BE()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public string ReadAscii(int length)
        {
            var data = base.ReadBytes(length);
            return Encoding.ASCII.GetString(data);
        }


        public void Move(long bytes)
        {
            var newPosition = BaseStream.Position + bytes;
            Goto(newPosition);
        }

        public void Goto(long newPosition)
        {
            if (newPosition < 0)
            {
                throw new Exception("Moved past start of file");
            }

            if (newPosition > BaseStream.Length)
            {
                throw new Exception("Moved past end of file");
            }

            BaseStream.Position = newPosition;
        }

        public bool EndOfStream()
        {
            if (BaseStream.Position == BaseStream.Length)
            {
                return true;
            }
            return false;
        }

    }
}
