using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeCompareSystem.Model
{
    class Barcode
    {
        public string[] Fields = new string[] { }; 
        public string[] UnCheckFields = new string[] { };
        //public string Sequence = "";
        //public int Serialization = 1;
        public string numberSerial = "";
        public string product_no = "";
        public string serial_sequence = "";

        public Barcode() {
        }

        public Barcode(string[] fields) {
            this.Fields = fields;
            //this.Sequence = sequence; 
        }
    }
}
