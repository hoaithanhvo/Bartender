using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodeCompareSystem.Util.Const;

namespace BarcodeCompareSystem.Model
{
    class TemplateField
    {
        public string Name;
        public string BtwValue;

        public TemplateField()
        {
        }

        public TemplateField(string name, string btwValue)
        {
            this.Name = name;
            this.BtwValue = btwValue;
        }
        //public string Name;
        //public string BtwValue;
        //public Const.FieldType Type;
        //public string Sequence;
        //public int Serialization;

        //public TemplateField() {
        //}

        //public TemplateField(string name, string btwValue, Const.FieldType type, string sequence, int serialization)
        //{
        //    this.Name = name;
        //    this.BtwValue = btwValue;
        //  //  this.DbValue = dbValue;
        //    this.Type = type;
        //    this.Sequence = sequence;
        //    this.Serialization = serialization;
        //}
    }
}
