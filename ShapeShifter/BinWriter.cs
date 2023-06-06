using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter
{
    public class BinWriter : BinaryWriter
    {
        public BinWriter(Stream stream) : base(stream) { }

        public long Position
        {
            get { return BaseStream.Position; }
        }

        public long Length
        {
            get { return BaseStream.Length; }
        }

        public void WriteBE(int value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }

        public void WriteBE(Int16 value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }

        public void WriteBE(UInt16 value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }

        public void WriteBE(Int64 value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }

        public void WriteBE(UInt64 value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }

        public void WriteBE(double value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }

        public void WriteBE(UInt32 value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }


        public void WriteAscii(string text, int length)
        {
            var output = new byte[length];
            var convertedText = Encoding.ASCII.GetBytes(text);
            for (var i = 0; i < length; i++)
            {
                if (i < convertedText.Length)
                    output[i] = convertedText[i];
                else
                    output[i] = 0;
            }

            this.Write(output);
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

