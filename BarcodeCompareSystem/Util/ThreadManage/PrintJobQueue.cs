using System.Threading;
using BarcodeCompareSystem.Model;
using BarcodeCompareSystem.ViewModel;

namespace BarcodeCompareSystem.Util
{
    class PrintJobQueue
    {
        public SizeQueue<PrintJob> _printQueue = new SizeQueue<PrintJob>(100);

        public bool KeepProcessing { get; set; }
        public EngineWrapper engineWrapper;

        public PrintJobQueue(EngineWrapper _engine)
        {
            this.engineWrapper = _engine;
        }

        /* For thread-safety use the SizeQueue from Marc Gravell (SO #5030228) */
        public void AddPrintItem(PrintJob printJob)
        {
            this._printQueue.Enqueue(printJob);
        }

        public void ProcessQueue()
        {
            this.KeepProcessing = true;
            while (this.KeepProcessing)
            {
                while (this._printQueue.Count() > 0)
                {
                    // Print value from queue
                    engineWrapper.Print(_printQueue.Dequeue());
                }

                Thread.CurrentThread.Join(2 * 1000); //2 secs
            }
        }
    }
}