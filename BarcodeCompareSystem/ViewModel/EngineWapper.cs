using System;
using System.IO;
using Seagull.BarTender.Print;
using BarcodeCompareSystem.Model;
using System.Collections.Generic;

namespace BarcodeCompareSystem.ViewModel
{

    class EngineWrapper : IDisposable
    {
        #region Delegates
        public delegate void DoUpdateOutputDelegate(string output);
        #endregion

        // Engine Field 
        private Engine m_engine = null;

        LabelFormatDocument _btFormat = null;

        private string _bartenderFormatFilePath;
        // This property creates and starts the engine the first time that it is 
        // called. Most methods in this class (and methods in child classes) 
        // should use this property instead of the m_engine field. 
        protected Engine BtEngine
        {
            get
            {
                // If the engine is not created yet, create and start it. 
                if (m_engine == null)
                {
                    m_engine = new Engine(true);
                }
                return m_engine;
            }
        }

        public EngineWrapper()
        {
            if (m_engine == null)
            {
                m_engine = new Engine(true);
            }

            // Sign up for print job events.
            m_engine.JobCancelled += new EventHandler<PrintJobEventArgs>(Engine_JobCancelled);
            m_engine.JobErrorOccurred += new EventHandler<PrintJobEventArgs>(Engine_JobErrorOccurred);
            m_engine.JobMonitorErrorOccurred += new EventHandler<MonitorErrorEventArgs>(Engine_JobMonitorErrorOccurred);
            m_engine.JobPaused += new EventHandler<PrintJobEventArgs>(Engine_JobPaused);
            m_engine.JobQueued += new EventHandler<PrintJobEventArgs>(Engine_JobQueued);
            m_engine.JobRestarted += new EventHandler<PrintJobEventArgs>(Engine_JobRestarted);
            m_engine.JobResumed += new EventHandler<PrintJobEventArgs>(Engine_JobResumed);
            m_engine.JobSent += new EventHandler<JobSentEventArgs>(Engine_JobSent);
        }


        // Open file with printer
        public void OpenFormat(string filepath, string printerName)
        {
            this._bartenderFormatFilePath = filepath;
            this._btFormat = this.m_engine.Documents.Open(this._bartenderFormatFilePath, printerName);
         
        }

        public string ExportImage(double width, double height) {
            string thumbnailFile = Path.GetTempFileName(); 
            this._btFormat.ExportImageToFile(thumbnailFile, ImageType.JPEG, Seagull.BarTender.Print.ColorDepth.ColorDepth24bit, new Resolution((int)width, (int)height), OverwriteOptions.Overwrite);
            return thumbnailFile;
        }

        // Open file with printer
        public void OpenFormat(string filepath)
        {
            this._bartenderFormatFilePath = filepath;
            this._btFormat = this.m_engine.Documents.Open(this._bartenderFormatFilePath);
            // this._btFormat = this._btEngine.Documents.Open(filepath);
        }

        // Close file
        public void CloseFormat(bool isSaving)
        {
            try {
                if (isSaving)
                {
                    this._btFormat.Close(SaveOptions.SaveChanges);
                }
                else
                {
                    this._btFormat.Close(SaveOptions.DoNotSaveChanges);
                }
            } catch
            {

            }
            
        }

        public Result Print(PrintJob printJob) {
            // Print
            //foreach (BarcodeField barcodeField in printJob.btwData.Barcodes)
            //{
            //    foreach (TemplateField field in barcodeField.Fields) {
            //        this.SetValue(field.Name, field.BtwValue);
            //    }
            //}

            this.OpenFormat(printJob.Path, printJob.Printer);
            this.SetPrintNumber(printJob.CopiesOfLabel, printJob.Serializiers);
            return this.PrintFormat();
        }

        // Messages
        public Result PrintFormat()
        {
            Messages messages = null;
            Result result = this._btFormat.Print("PrintJob1", out messages);
            return result;
        }

        // Setup print parameter 
        public void SetPrintNumber(int copiesNo, int serNo)
        {
            this._btFormat.PrintSetup.IdenticalCopiesOfLabel = copiesNo;
            this._btFormat.PrintSetup.NumberOfSerializedLabels = serNo; 
            }

        // Read value from bartender
        public string ReadValue(string objectName)
        {
            if (objectName == "") {
                return "";
            }

            return this._btFormat.SubStrings[objectName].Value;
        }

        // Read value from bartender
        public void SetValue(string objectName, string value)
        {
            this._btFormat.SubStrings[objectName].Value = value;
        }

        public Seagull.BarTender.Print.SubString GetSubString(string objectName) {
            return this._btFormat.SubStrings[objectName];
        }

        #region Print Job Event Handlers
        /// <summary>
        /// Handle the print events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="printJob"></param>
        void Engine_JobSent(object sender, JobSentEventArgs printJob)
        {
            if (printJob.JobPrintingVerified)
                Console.WriteLine(string.Format("PrintJob {0} Sent/Print Verified on {1}.", printJob.Name, printJob.PrinterInfo.Name));
            else
                Console.WriteLine("PrintJob {0} Sent to {1}.", printJob.Name, printJob.PrinterInfo.Name);
        }

        void Engine_JobResumed(object sender, PrintJobEventArgs printJob)
        {
            Console.WriteLine(string.Format("PrintJob {0} Resumed.", printJob.Name));
        }

        void Engine_JobRestarted(object sender, PrintJobEventArgs printJob)
        {
            Console.WriteLine(string.Format("PrintJob {0} Restarted on {1}.", printJob.Name, printJob.PrinterInfo.Name));
        }

        void Engine_JobQueued(object sender, PrintJobEventArgs printJob)
        {
            Console.WriteLine(string.Format("PrintJob {0} Queued on {1}.", printJob.Name, printJob.PrinterInfo.Name));
        }

        void Engine_JobPaused(object sender, PrintJobEventArgs printJob)
        {
            Console.WriteLine(string.Format("PrintJob {0} Paused.", printJob.Name));
        }

        void Engine_JobMonitorErrorOccurred(object sender, MonitorErrorEventArgs errorInfo)
        {
            Console.WriteLine(string.Format("Job Monitor Error {0}.", errorInfo.Message));
        }

        void Engine_JobErrorOccurred(object sender, PrintJobEventArgs printJob)
        {
            Console.WriteLine(string.Format("PrintJob {0} Error {1}.", printJob.Name, printJob.PrinterInfo.Message));
        }

        void Engine_JobCancelled(object sender, PrintJobEventArgs printJob)
        {
            Console.WriteLine(string.Format("PrintJob {0} Cancelled.", printJob.Name));
        }
        #endregion


        #region Methods
        /// <summary>
        /// Add our status string to the list box.
        /// </summary>
        /// <param name="output"></param>
        private void DoUpdateOutput(string output)
        {
        }
        #endregion

        // Implement IDisposable 
        public void Dispose()
        {
            // The engine needs to be stopped and disposed only if it was 
            // created. Use the field here, not the property. Otherwise, 
            // you might create a new instance in the Dispose method. 
            if (m_engine != null)
            {
                // Stop the process and release Engine field resources. 
                m_engine.Stop();
                m_engine.Dispose();
            }
        }
        // Additional methods for specific work in your application. All additional 
        // methods should use the BtEngine property instead of the m_engine field. 
    }
}

