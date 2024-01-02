using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeCompareSystem.Model
{
    class BtwData
    {
        public string Path; 
        public string FileName;
        public List<BarcodeField> Barcodes = new List<BarcodeField>();
       

        public BtwData() {
        }

        public BtwData(string path, string fileName, List<BarcodeField> barcodeFields) {
            this.Path = path;
            this.FileName = fileName;
            this.Barcodes = barcodeFields;
        }
    }

    class BarcodeField {
        public List<TemplateField> Fields = new List<TemplateField>();
        public List<TemplateField> UncheckFields = new List<TemplateField>();
        public string Sequence = "";
        //public int Serialization = 1; 

        public BarcodeField(List<TemplateField> fields) {
            this.Fields = fields;
            //this.Sequence = sequence;
        }

        public BarcodeField() { }
    }
}
