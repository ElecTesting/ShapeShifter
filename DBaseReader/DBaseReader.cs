using System.Collections;
using System.Data.Common;
using System.Text;

namespace DBaseReader
{
    /* dBase Data Reader
     * 
     * A data reader for dBase files which allows for only reading the data you need
     * All other dBase libraries I found loaded the entire file into memory
     * 
     */
    public class DBaseReader : DbDataReader
    {
        private byte _version;
        private string _lastUpdated = "";
        private int _headerLength;
        private int _recordLength;
        private long _recordPosition = 0;
        private long _dataPosition = 0;

        public List<DBaseField> Columns { get; set; } = new List<DBaseField>();
        public int RowNumber { get; set; } = 0;
        public uint RowCount { get; set; } = 0;

        private BinReader _reader;

        public DBaseReader(string path)
        {
            // read dbase header info
            _reader = new BinReader(new FileStream(path, FileMode.Open, FileAccess.Read));
            _version = _reader.ReadByte();
            var lastUpdated = _reader.ReadBytes(3);
            RowCount = _reader.ReadUInt32();
            _headerLength = _reader.ReadUInt16();
            _recordLength = _reader.ReadUInt16();
            var reserved = _reader.ReadBytes(3);
            var reserved2 = _reader.ReadBytes(13);
            var reserved3 = _reader.ReadBytes(4);

            // read field descriptors
            CollectFields();
            _recordPosition = _reader.Position;
        }

        private void CollectFields()
        {
            while (true)
            {
                var field = new DBaseField();
                field.Name = _reader.ReadAscii(11);
                if (field.Name.Substring(0,1) == "\r")
                {
                    // rewind this read to the start of the data
                    _reader.Move(-9);
                    _dataPosition = _reader.Position;
                    break;
                }
                field.Name = field.Name.Replace("\0", "");
                field.Type = (FieldType)_reader.ReadChar();
                field.Address = _reader.ReadInt32();
                field.Length = _reader.ReadByte();
                field.DecimalCount = _reader.ReadByte();
                var reserved1 = _reader.ReadBytes(2);
                var workArea = _reader.ReadByte();
                var reserved2 = _reader.ReadBytes(2);
                var flag = _reader.ReadByte();
                var reserved3 = _reader.ReadBytes(8);
                Columns.Add(field);
            }
        }

        public override void Close()
        {
            _reader.Close();
            _reader.Dispose();
        }

        public override object this[int ordinal] => throw new NotImplementedException();

        public override object this[string name] => throw new NotImplementedException();

        public override int Depth => throw new NotImplementedException();

        public override int FieldCount => throw new NotImplementedException();

        public override bool HasRows 
        { 
            get 
            {
                if (RowNumber < RowCount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            } 
        }

        public override bool IsClosed => throw new NotImplementedException();

        public override int RecordsAffected => throw new NotImplementedException();

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(string fieldName)
        {
            return GetDouble(GetOrdinal(fieldName));
        }

        public override double GetDouble(int ordinal)
        {
            var data = GetFieldData(ordinal);
            var stringData = Encoding.ASCII.GetString(data);
            return Convert.ToDouble(stringData);
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(string fieldName)
        {
            return GetInt32(GetOrdinal(fieldName));
        }

        public override int GetInt32(int ordinal)
        {
            var data = GetFieldData(ordinal);
            var stringData = Encoding.ASCII.GetString(data);
            return Convert.ToInt32(stringData);
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetName(int ordinal)
        {
            if (ordinal > Columns.Count)
            {
                throw new Exception($"Oridinal out of range");
            }
            return Columns[ordinal].Name;
        }

        public override int GetOrdinal(string name)
        {
            var field = Columns.Where(f => f.Name == name).FirstOrDefault();
            if (field == null)
            {
                throw new Exception($"Field {name} not found");
            }
            return Columns.IndexOf(field);
        }

        public override string GetString(int ordinal)
        {
            var data = GetFieldData(ordinal);
            return Encoding.ASCII.GetString(data).TrimEnd();
        }

        public string GetString(string fieldName)
        {
            var ordinal = GetOrdinal(fieldName);
            return GetString(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override bool NextResult()
        {
            if (RowNumber < RowCount)
            {
                _recordPosition += _recordLength;
                RowNumber++;
                return true;
            }
            return false;
        }

        public override bool Read()
        {
            throw new NotImplementedException();
        }

        /* gets the raw field data from an ordinal
         * returns a byte array
         */
        private byte[] GetFieldData(int ordinal)
        {
            var fieldCount = 0;
            var fieldOffset = 0;
            foreach (var thisField in Columns)
            {
                if (fieldCount == ordinal)
                {
                    _reader.Goto(_recordPosition + fieldOffset);
                    var data = _reader.ReadBytes(thisField.Length);
                    return data;
                }
                fieldOffset += thisField.Length;
                fieldCount++;
            }
            throw new Exception("Data not found");
        }

        /* Moves the reader to a specific row number
         */
        public void GotoRow(int rowNumber)
        {
            if (rowNumber > RowCount)
            {
                throw new Exception($"Row {rowNumber} not found");
            }
            _recordPosition = _dataPosition + (rowNumber * _recordLength);
            RowNumber = rowNumber;
        }
    }
}