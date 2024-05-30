using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodeCompareSystem.ViewModel;

namespace BarcodeCompareSystem.Model
{
    class PrintJob
    {
        public BtwData btwData = new BtwData();
        public int CopiesOfLabel { get; set; } = 1 ;
        public int Serializiers = 1;
        public string Printer;
        public string Path; 

        public PrintJob() {
        }

        public PrintJob(BtwData data, int copiesOfLabel, int serializers, string printer, string path) {
            this.btwData = data;
            this.CopiesOfLabel = copiesOfLabel;
            this.Serializiers = serializers;
            this.Printer = printer;
            this.Path = path;
        }
    }
}
