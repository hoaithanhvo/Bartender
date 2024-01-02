using System;
using System.Runtime.Serialization;

namespace BarcodeCompareSystem
{

    [Serializable]
    public class DBException : Exception
    {
        public DBException()
        { }

        public DBException(string message)
            : base(message)
        { }

        public DBException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
