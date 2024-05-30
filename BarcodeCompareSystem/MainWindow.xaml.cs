using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Drawing.Printing;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Collections;
using System.Windows.Input;
using BarcodeCompareSystem.ViewModel;
using System.Windows.Media.Imaging;
using BarcodeCompareSystem.Util;
using BarcodeCompareSystem.Model;
using System.Threading;
using System.Diagnostics;
using IniParser.Model;
using IniParser;
using System.Data;
using System.Collections.ObjectModel;
using Seagull.BarTender.Print;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Globalization;
using System.Windows.Threading;
using System.Drawing;
using System.Data.SqlClient;
using System.Security.AccessControl;

namespace BarcodeCompareSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {

        public bool FlagCheck;
        private EngineWrapper _bartenderEngine;
        private IList<string> _printers;
        private string _basePath;
        private string thumbnailFile = "";
        private FolderBrowserDialog _folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
        private Hashtable listItems;
        private PrintJob _printJob = new PrintJob();
        private BtwData _btwData;
        private PrintJobQueue printJobQueue;
        private string[] browsingFormats;
        private string SERVER_NOT_VALID = "Serial is not valid!";
        private string ZERO = "0";
        private string configPath;
        private string pathGetFile;
        const string CONFIG_FILE = "config.ini";
        string Department;
        public string[] ModelYear2NumberArray;
        public string checkNumberSerialConfig;
        string serial_sequence;
        public string dateRessult;
        static bool isBCalled = false;
        private string checkFileName;
        private string weekOfYear;
        public string txt_Datecode4Number;
        public string txt_Datecode3Number;
        public string dayYearPart;
        public string shift_cd;
        public string GoogleNumber;
        public IList<string> Printers
        {
            get { return _printers; }
            set
            {
                _printers = value;
                OnPropertyChanged("Printers");
            }
        }

        public DateTime time;
        private DispatcherTimer timer;
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Hàm sẽ được gọi lại sau mỗi giờ
            this.Refresh_Form();// Thay YourFunction bằng tên hàm của bạn
            ScheduleNextRun();
        }
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            this.Printers = this.GetInstalledPrinters();
            this.DataContext = this;
            this.unCheckFieldStackPanel.IsEnabled = false;
            this._bartenderEngine = new EngineWrapper();

            // Initialize a list and a queue.
            listItems = new System.Collections.Hashtable();
            printJobQueue = new PrintJobQueue(_bartenderEngine);
            //thanh add load file from path config.init
            timer = new DispatcherTimer();
            DateTime now = DateTime.Now;
            DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day, 6, 30, 0);


            // Nếu thời điểm đã qua, thì lên lịch cho ngày hôm sau
            if (now > scheduledTime)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            ScheduleNextRun();
            LoadFilePath();
        }

        private void ScheduleNextRun()
        {
            DateTime now = DateTime.Now;
            DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day, 6, 30, 0);

            // Nếu thời điểm đã qua, thì lên lịch cho 6:30 sáng ngày hôm sau
            if (now > scheduledTime)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            // Tính khoảng thời gian chờ đến lần gọi tiếp theo
            TimeSpan timeUntilNextRun = scheduledTime - now;

            // Cập nhật khoảng thời gian cho timer và khởi động lại timer
            timer.Interval = timeUntilNextRun;
            timer.Start();
        }
        public void LoadFilePath()
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(directory + "\\" + CONFIG_FILE);
                pathGetFile = data["Database"]["Path"];
                Department = data["Database"]["Department"];
                string ModelYear2Number = data["Database"]["ModelYear2Number"];
                // Chia chuỗi thành mảng
                ModelYear2NumberArray = ModelYear2Number.Split(',');

                this.listItems.Clear();
                this.txtModel.Text = "";
                this.browsingFormats = System.IO.Directory.GetFiles(pathGetFile, "*.btw");
                this.lstLabelBrowser.ItemsSource = this.ParserFileList(browsingFormats);
                DateTime currentTime = DateTime.Now;
                if (currentTime.Hour >= 7 || currentTime.Hour < 4)
                {
                    Properties.Settings.Default.FlagCheck = true;
                    Properties.Settings.Default.Save();
                }
                DBAgent db1 = DBAgent.Instance;


                // Assuming GetData returns a DataTable with a single row and single column
                DataTable resultTable = db1.GetData("SELECT GETDATE();");

                if (resultTable.Rows.Count > 0 && resultTable.Columns.Count > 0)
                {
                    // Extract the DateTime value from the first row, first column
                    time = (DateTime)resultTable.Rows[0][0];
                    // Now 'time' contains the DateTime value from the database
                }


                // Kiểm tra nếu là 7 giờ sáng và chưa kiểm tra trong ngày nay

                // Nếu là 4 giờ sáng, reset giá trị Count

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Không tìm thấy file trong config", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //throw new DBException("");
            }
        }
        private void Button_Open_Model(object sender, EventArgs e)
        {

            this.listItems.Clear();
            //ObservableCollection<object> fileList = new ObservableCollection<object>();

            //ket noi sql M_BarTender, load Model from sql
            string Model = txtModel.Text;
            try
            {
                DBAgent db = DBAgent.Instance;

                DataTable dt = new DataTable();
                dt = db.GetData("Select * from M_BARTENDER where PRODUCT_NO=@nme;", new Dictionary<string, object> { { "@nme", Model } });

                if (dt != null && dt.Rows.Count > 0)
                {

                    var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(directory + "\\" + CONFIG_FILE);
                    pathGetFile = data["Database"]["Path"];


                    this.listItems.Clear();

                    List<string> matchingFilesList = new List<string>(); // Tạo một danh sách tạm để lưu các tệp được lọc
                    foreach (DataRow row in dt.Rows)
                    {
                        string nameFile = row["FILENAME"].ToString().Trim();

                        // Lọc danh sách các tệp theo tên tệp từ cột "FILENAME"
                        var matchingFiles = System.IO.Directory.GetFiles(pathGetFile, "*.btw")
                                                .Where(filePath => System.IO.Path.GetFileName(filePath) == nameFile);

                        // Thêm các tệp đã lọc vào danh sách tạm
                        matchingFilesList.AddRange(matchingFiles);
                    }

                    // Gán mảng đã lọc vào this.browsingFormats
                    this.browsingFormats = matchingFilesList.ToArray();

                    // Gán danh sách đã lọc vào lstLabelBrowser.ItemsSource
                    this.lstLabelBrowser.ItemsSource = this.ParserFileList(this.browsingFormats);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Product", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Console.WriteLine("No data found for the given PRODUCT_NO.");
                }
            }
            catch (Exception error)
            {
                System.Windows.Forms.MessageBox.Show("Có lỗi xảy ra. Liên hệ System!" + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //App.DebugLog(error.StackTrace);
            }
        }
        private List<FileBox> ParserFileList(string[] files)
        {
            List<FileBox> fileInfos = new List<FileBox>();
            int count = 0;
            foreach (string file in files)
            {
                count = count + 1;
                string fileTitle = "[" + count.ToString() + "] " + Path.GetFileName(file);
                FileBox fileInfo = new FileBox(fileTitle, file);
                fileInfos.Add(fileInfo);
            }
            return fileInfos;
        }
        public string OneYearTwoWeek(bool? check)
        {
            DateTime TimeNow = DateTime.Now;
            DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
            if (TimeNow < sixThirtyAM && check==true)
            {
                TimeNow = TimeNow.AddDays(-1);
            }
            int year = TimeNow.Year;
            CultureInfo ci = CultureInfo.CurrentCulture;
            Calendar calendar = ci.Calendar;
            weekOfYear = calendar.GetWeekOfYear(TimeNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();
            int lastTwoDigitsOfYear = year % 10;
            string result = lastTwoDigitsOfYear.ToString() + weekOfYear;
            return result;
        }
        public string TwoYearTwoWeek(bool? check)
        {
            DateTime TimeNow = DateTime.Now;
            DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
            if (TimeNow < sixThirtyAM && check==true)
            {
                TimeNow = TimeNow.AddDays(-1);
            }
            int year = TimeNow.Year;
            CultureInfo ci = CultureInfo.CurrentCulture;
            Calendar calendar = ci.Calendar;
            weekOfYear = calendar.GetWeekOfYear(TimeNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();
            int lastTwoDigitsOfYear = year % 100;
            string result = lastTwoDigitsOfYear.ToString() + weekOfYear;
            return result;
        }

        public void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var item = sender as System.Windows.Controls.ListViewItem;
                if (item != null)
                {
                    FileBox fInfo = item.DataContext as FileBox;
                    this._basePath = fInfo.FilePath;
                    this.lstLabelBrowser.IsEnabled = false;
                    this.ClearStackPanel();

                    new Thread(this.Loading).Start();
                }

                else
                {
                    // Clear any previous image.
                    picThumbnail.DataContext = "";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.StackTrace);
                this.lstLabelBrowser.IsEnabled = true;

            }
        }
        public string formatDate4serial(DateTime dateRaw)
        {
            DateTime dateTime = DateTime.Now;
            DateTime sixThirtyAM = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 6, 30, 0);
            if (dateTime < sixThirtyAM)
            {
                dateTime = dateTime.AddDays(-1);
            }

            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;


            string lastDigitOfYear = (year % 10).ToString();

            string monthCode;
            if (month >= 1 && month <= 9)
            {
                monthCode = month.ToString();
            }
            else if (month == 10)
            {
                monthCode = "A";
            }
            else if (month == 11)
            {
                monthCode = "B";
            }
            else if (month == 12)
            {
                monthCode = "C";
            }
            else
            {
                throw new ArgumentOutOfRangeException("month", "Tháng không hợp lệ.");
            }

            char lastDigitOfYearChar = lastDigitOfYear[lastDigitOfYear.Length - 1];
            char monthCodeChar = monthCode[0];

            char[] dayCodes = "ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();

            string result;
            char dayCode;
            if (day >= 10)
            {
                dayCode = dayCodes[day - 10];
                result = lastDigitOfYearChar.ToString() + monthCodeChar.ToString() + dayCode.ToString();
            }
            else
            {
                result = lastDigitOfYearChar.ToString() + monthCodeChar.ToString() + day.ToString();
            }
            return result;
        }
        public string FormatDate4serialInventory(DateTime dateTime)
        {
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;


            string lastDigitOfYear = (year % 10).ToString();

            string monthCode;
            if (month >= 1 && month <= 9)
            {
                monthCode = month.ToString();
            }
            else if (month == 10)
            {
                monthCode = "A";
            }
            else if (month == 11)
            {
                monthCode = "B";
            }
            else if (month == 12)
            {
                monthCode = "C";
            }
            else
            {
                throw new ArgumentOutOfRangeException("month", "Tháng không hợp lệ.");
            }

            char lastDigitOfYearChar = lastDigitOfYear[lastDigitOfYear.Length - 1];
            char monthCodeChar = monthCode[0];

            char[] dayCodes = "ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();

            string result;
            char dayCode;
            if (day >= 10)
            {
                dayCode = dayCodes[day - 10];
                result = lastDigitOfYearChar.ToString() + monthCodeChar.ToString() + dayCode.ToString();
            }
            else
            {
                result = lastDigitOfYearChar.ToString() + monthCodeChar.ToString() + day.ToString();
            }
            return result;
        }
        public string formatDate2YMD()
        {
            DateTime dateTime = DateTime.Now;
            DateTime sixThirtyAM = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 6, 30, 0);
            if (dateTime < sixThirtyAM)
            {
                dateTime = dateTime.AddDays(-1);
            }
            string Year = dateTime.Year.ToString().Substring(2); // Lấy hai ký tự cuối của năm
            string Month = dateTime.Month.ToString("D2"); // Đảm bảo tháng có hai ký tự
            string Day = dateTime.Day.ToString("D2"); // Đảm bảo ngày có hai ký tự
            string formattedDate = $"{Year}{Month}{Day}"; // Định dạng lại chuỗi
            return formattedDate;
        }
        public string formatDate2YMDInventory()
        {
            DateTime dateTime = DateTime.Now;
            string Year = dateTime.Year.ToString().Substring(2); // Lấy hai ký tự cuối của năm
            string Month = dateTime.Month.ToString("D2"); // Đảm bảo tháng có hai ký tự
            string Day = dateTime.Day.ToString("D2"); // Đảm bảo ngày có hai ký tự
            string formattedDate = $"{Year}{Month}{Day}"; // Định dạng lại chuỗi
            return formattedDate;
        }

        public string formatDateToRePrint(string date)
        {
            if (DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                int lastDigitOfYear;
                if (ModelYear2NumberArray.Contains(checkNumberSerialConfig))
                {
                    lastDigitOfYear = parsedDate.Year % 100;
                }
                else
                {
                    lastDigitOfYear = parsedDate.Year % 10;
                }
                string monthCode;
                if (parsedDate.Month == 10)
                {
                    monthCode = "X";
                }
                else if (parsedDate.Month == 11)
                {
                    monthCode = "Y";
                }
                else if (parsedDate.Month == 12)
                {
                    monthCode = "Z";
                }
                else
                {
                    monthCode = parsedDate.Month.ToString();
                }

                string dayYearPart = $"{lastDigitOfYear}{monthCode}{parsedDate.Day:D2}";
                return dayYearPart;
            }
            else
            {
                return "Invalid Date";
            }
        }
        public string formatDateNow()
        {
            DateTime TimeNow = DateTime.Now;
            DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
            if (TimeNow < sixThirtyAM)
            {
                TimeNow = TimeNow.AddDays(-1);
            }
            int lastDigitOfYear;
            if (ModelYear2NumberArray.Contains(checkNumberSerialConfig))
            {
                lastDigitOfYear = TimeNow.Year % 100;
            }
            else
            {
                lastDigitOfYear = TimeNow.Year % 10;
            }

            string monthCode;
            if (TimeNow.Month == 10)
            {
                monthCode = "X";
            }
            else if (TimeNow.Month == 11)
            {
                monthCode = "Y";
            }
            else if (TimeNow.Month == 12)
            {
                monthCode = "Z";
            }
            else
            {
                monthCode = TimeNow.Month.ToString();
            }

            string dayYearPart = $"{lastDigitOfYear}{monthCode}{TimeNow.Day:D2}";
            return dayYearPart;

        }
        public string formatDateToInventory(DateTime dateRaw)
        {
            DateTime TimeNow = dateRaw;
            int lastDigitOfYear;
            if (ModelYear2NumberArray.Contains(checkNumberSerialConfig))
            {
                lastDigitOfYear = TimeNow.Year % 100;
            }
            else
            {
                lastDigitOfYear = TimeNow.Year % 10;
            }

            string monthCode;
            if (TimeNow.Month == 10)
            {
                monthCode = "X";
            }
            else if (TimeNow.Month == 11)
            {
                monthCode = "Y";
            }
            else if (TimeNow.Month == 12)
            {
                monthCode = "Z";
            }
            else
            {
                monthCode = TimeNow.Month.ToString();
            }

            string dayYearPart = $"{lastDigitOfYear}{monthCode}{TimeNow.Day:D2}";
            return dayYearPart;

        }
        public void LoadBtwData()
        {
            try
            {
                DBAgent dbBoxNumber = DBAgent.Instance;
                DBAgent dbBoxNumberStart = DBAgent.Instance;
                DataTable dtBoxNumberStart = dbBoxNumberStart.GetData(
                                   "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                   new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) }
                                   }
                                );
                serial_sequence = "";
                this._bartenderEngine.OpenFormat(this._basePath);
                this._btwData = new BtwData();
                this._btwData.FileName = Path.GetFileName(this._basePath);
                checkNumberSerialConfig = this._btwData.FileName.Substring(0, this._btwData.FileName.Length - 4);

                DateTime TimeNow = DateTime.Now;
                DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
                DateTime seventeenThirtyPM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 18, 30, 0);
                shift_cd = (TimeNow < sixThirtyAM || TimeNow > seventeenThirtyPM) ? "CA3" : "CA1";
                isBCalled = shift_cd == "CA3" ? false : true;
                this._btwData.Path = this._basePath;
                int a = dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim().Length;
                dayYearPart = dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim().Length == 5 ? formatDateNow() : formatDate4serial(DateTime.Now);
                //thanh98 add load data sql
                List<Barcode> barcodesIni = ConfigFile.GetFields(this._btwData.FileName, ModelYear2NumberArray, dayYearPart);
                BarcodeField barcodeFields;
                string dayYearPartScreen = dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim().Length == 5 ? formatDateNow() + " | " + shift_cd : formatDate4serial(DateTime.Now) + " | " + shift_cd;
                foreach (Barcode barcode in barcodesIni)
                {
                    barcodeFields = new BarcodeField();
                    foreach (string fieldName in barcode.Fields)
                    {
                        TemplateField field = new TemplateField();
                        string value = this._bartenderEngine.ReadValue(fieldName);
                        field.Name = fieldName;
                        field.BtwValue = value;
                        barcodeFields.Fields.Add(field);
                    }

                    //thanh cmt đọc giá trị các trường uncheckFields
                    foreach (string fieldName in barcode.UnCheckFields)
                    {
                        if (fieldName != "txt_Serial")
                        {
                            TemplateField field = new TemplateField();
                            string value = this._bartenderEngine.ReadValue(fieldName);
                            field.Name = fieldName;
                            field.BtwValue = fieldName == "txt_Datecode" ? dayYearPartScreen : value;
                            //field.BtwValue = fieldName == "txt_Datecode4Number" ? TwoYearTwoWeek() : value;
                            //field.BtwValue = fieldName == "txt_Datecode3Number" ? TwoYearTwoWeek() : value;
                            if (fieldName == "txt_Datecode3Number")
                            {
                                field.BtwValue = OneYearTwoWeek(true);
                            }
                            if (fieldName == "txt_Datecode4Number")
                            {
                                field.BtwValue = TwoYearTwoWeek(true);
                            }
                            if (fieldName == "txt_GoogleNumber")
                            {
                                string readGoogleNumber = this._bartenderEngine.ReadValue(fieldName);
                                GoogleNumber = readGoogleNumber;
                            }
                            barcodeFields.UncheckFields.Add(field);

                        }
                        else
                        {
                            //lay serial hien tai + 1
                            serial_sequence = barcode.serial_sequence;
                            TemplateField field = new TemplateField();
                            field.Name = fieldName;
                            //field.BtwValue = barcode.numberSerial;
                            field.BtwValue = barcode.numberSerial;
                            barcodeFields.UncheckFields.Add(field);
                        }
                    }
                    this._btwData.Barcodes.Add(barcodeFields);
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Không tải được dữ liệu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                App.DebugLog(e.StackTrace);
            }
        }
        private void RenderUIUsingBtwData()
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    thumbnailFile = this._bartenderEngine.ExportImage(700, 1000);
                    picThumbnail.Source = new BitmapImage(new Uri(thumbnailFile));
                }
                catch (Exception e)
                {
                }

                try
                {
                    foreach (BarcodeField barcode in this._btwData.Barcodes)
                    {
                        // thanh add allow edit fileds
                        foreach (TemplateField field in barcode.Fields)
                        {
                            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                            label.Content = field.Name;
                            label.FontSize = 16;
                            label.Margin = new System.Windows.Thickness(20, 20, 20, 5);
                            label.FontWeight = FontWeights.Bold;
                            this.checkFieldStackPanel.Children.Add(label);
                            System.Windows.Controls.TextBox tbxValue = new System.Windows.Controls.TextBox();
                            tbxValue.Name = "_name_" + field.Name;
                            tbxValue.Text = field.BtwValue;
                            tbxValue.IsEnabled = true;
                            tbxValue.FontSize = 20;
                            tbxValue.BorderBrush = System.Windows.Media.Brushes.Black;
                            tbxValue.FontWeight = FontWeights.Bold;
                            tbxValue.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                            tbxValue.Margin = new System.Windows.Thickness(20, 5, 20, 10);
                            this.checkFieldStackPanel.Children.Add(tbxValue);
                            this.checkFieldStackPanel.RegisterName(tbxValue.Name, tbxValue);
                        }

                        // thanh add auto inscrease 
                        foreach (TemplateField field in barcode.UncheckFields)
                        {
                            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                            label.Content = field.Name;
                            label.FontSize = 16;
                            label.Margin = new System.Windows.Thickness(20, 20, 20, 5);
                            label.FontWeight = FontWeights.Bold;

                            this.unCheckFieldStackPanel.Children.Add(label);
                            System.Windows.Controls.TextBox tbxValue = new System.Windows.Controls.TextBox();
                            tbxValue.Name = "_name_" + field.Name;
                            tbxValue.Text = field.BtwValue;
                            tbxValue.IsEnabled = true;
                            //if (field.Name == "txt_Serial")
                            //{
                            //    tbxValue.Visibility = System.Windows.Visibility.Hidden;
                            //    label.Visibility = System.Windows.Visibility.Hidden;
                            //}

                            tbxValue.FontSize = 20;
                            tbxValue.BorderBrush = System.Windows.Media.Brushes.Black;
                            tbxValue.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                            tbxValue.FontWeight = FontWeights.Bold;
                            tbxValue.Margin = new System.Windows.Thickness(20, 5, 20, 10);
                            this.unCheckFieldStackPanel.Children.Add(tbxValue);
                            this.unCheckFieldStackPanel.RegisterName(tbxValue.Name, tbxValue);

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }

                this.Refresh_Button.IsEnabled = true;
                this.lstLabelBrowser.IsEnabled = true;
            });
        }
        private void Button_Re_Print_Click(object sender, RoutedEventArgs e)
        {
            LoginForm login = new LoginForm();
            if (login.ShowDialog() == true)
            {
                if (login.DialogResult == true)
                {

                    switch (Authenticate.IsUserExist(login.txtUsername.Text, login.txtPassword.Password))
                    {
                        case 0:
                            System.Windows.MessageBox.Show("Tài khoản không tồn tại!");
                            return;
                        case 2:
                            System.Windows.MessageBox.Show("Mật khẩu không chính xác!");
                            return;
                        case 1:
                            this.RePrint(login.txtUsername.Text);
                            login.Close();
                            return;
                    }
                }
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this._bartenderEngine != null)
            {
                this._bartenderEngine.Dispose();
            }
        }

        private string CV60E(string currentNumber, int labelCount)
        {
            char[] hangChucNghin = serial_sequence.Trim().ToCharArray();
            char[] hangNghin = serial_sequence.Trim().ToCharArray();
            char[] hangTram = serial_sequence.Trim().ToCharArray();
            char[] hangChuc = serial_sequence.Trim().ToCharArray();
            //char[] hangDonVi = serial_sequence.Substring(1).Trim().ToCharArray();
            char[] hangDonVi = serial_sequence.Trim().ToCharArray();


            bool checkCompare = false;
            int count = 0;
            //xu ly
            for (int m = 0; m < hangChucNghin.Length; m++)
            {
                for (int n = 0; n < hangNghin.Length; n++)
                {
                    for (int i = 0; i < hangTram.Length; i++)
                    {
                        for (int j = 0; j < hangChuc.Length; j++)
                        {
                            for (int k = 0; k < hangDonVi.Length; k++)
                            {
                                string A = $"{hangChucNghin[m]}{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}";

                                if (A == currentNumber)
                                {
                                    checkCompare = true;
                                    Console.WriteLine($"{hangChucNghin[m]}{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                                }
                                if (checkCompare == true)
                                {
                                    if (count == labelCount)
                                    {
                                        Console.WriteLine($"{hangChucNghin[m]}{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                                        string result = $"{hangChucNghin[m]}{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}";
                                        return result;
                                    }
                                    count++;
                                }

                            }
                        }
                    }
                }
            }
            return "0";
        }
        private string V12E(string currentNumber, int labelCount)
        {
            char[] hangTram = serial_sequence.Trim().ToCharArray();
            char[] hangChuc = serial_sequence.Trim().ToCharArray();
            char[] hangDonVi = serial_sequence.Substring(1).Trim().ToCharArray();

            bool checkCompare = false;
            int count = 0;
            //xu ly

            for (int i = 0; i < hangTram.Length; i++)
            {
                for (int j = 0; j < hangChuc.Length; j++)
                {
                    for (int k = 0; k < hangDonVi.Length; k++)
                    {
                        string A = $"{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}";

                        if (A == currentNumber)
                        {
                            checkCompare = true;
                            Console.WriteLine($"{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                        }
                        if (checkCompare == true)
                        {
                            if (count == labelCount)
                            {
                                Console.WriteLine($"{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                                string result = $"{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}";
                                return result;
                            }
                            count++;
                        }

                    }
                }
            }
            return "0";
        }
        private string V80E(string currentNumber, int labelCount)
        {
            char[] hangNghin = serial_sequence.Trim().ToCharArray();
            char[] hangTram = serial_sequence.Trim().ToCharArray();
            char[] hangChuc = serial_sequence.Trim().ToCharArray();
            //char[] hangDonVi = serial_sequence.Substring(1).Trim().ToCharArray();
            char[] hangDonVi = serial_sequence.Trim().ToCharArray();


            bool checkCompare = false;
            int count = 0;
            //xu ly

            for (int n = 0; n < hangNghin.Length; n++)
            {
                for (int i = 0; i < hangTram.Length; i++)
                {
                    for (int j = 0; j < hangChuc.Length; j++)
                    {
                        for (int k = 0; k < hangDonVi.Length; k++)
                        {
                            string A = $"{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}";

                            if (A == currentNumber)
                            {
                                checkCompare = true;
                                Console.WriteLine($"{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                            }
                            if (checkCompare == true)
                            {
                                if (count == labelCount)
                                {
                                    Console.WriteLine($"{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                                    string result = $"{hangNghin[n]}{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}";
                                    return result;
                                }
                                count++;
                            }

                        }
                    }
                }
            }

            return "0";
        }

        public void Print()
        {
            if (this._basePath != null)
            {
                try
                {
                    try
                    {
                        int numberLabelInt = int.Parse(CopiesOfLabel.Text);
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show("Number Label phải là số!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    this._bartenderEngine.OpenFormat(this._basePath);
                    string txtLot = "";
                    string txtProduct = "";
                    string txtDatecode = "";

                    string txtSerialStartUpdate = "";
                    string txtSerialUpdate = "";
                    string txtSerialStartInsert = "";
                    string txtSerialInsert = "";

                    var txtDate = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Datecode");
                    var txtBoxLot = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Lot");
                    var txtGoogleNumber = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_GoogleNumber");
                    var txt_Serial = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Serial");
                    var txt_Production = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Product");

                    if (CheckProduction(Path.GetFileName(this._basePath), txtBoxLot.Text))
                    {
                        //chọn may in

                        if (this.ComboBoxPrintersList.Text != "" && this.ComboBoxPrintersList.Text != null)
                        {
                            bool flagUpdate = false;
                            DBAgent dbBoxNumber = DBAgent.Instance;
                            DataTable dtBoxNumber = new DataTable();
                            isBCalled = false;
                            DateTime TimeNow = DateTime.Today;
                            int year = TimeNow.Year;
                            CultureInfo ci = CultureInfo.CurrentCulture;
                            Calendar calendar = ci.Calendar;
                            DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
                            if (TimeNow < sixThirtyAM)
                            {
                                TimeNow = TimeNow.AddDays(-1);
                            }
                            weekOfYear = calendar.GetWeekOfYear(TimeNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();
                            int lastTwoDigitsOfYear = year % 100;
                            int lastOneDigitsOfYear = year % 100;
                            checkFileName = Path.GetFileName(this._basePath);

                            //dayYearPart = txt_Serial.Text.Length == 5 ? formatDateNow() : formatDate4serial(DateTime.Now);
                            dayYearPart = txt_Serial?.Text?.Length == 5 ? formatDateNow() : formatDate4serial(DateTime.Now);

                            if (checkFileName == "V40W12BS2P5-07A021 NCVH THUNG(2).btw")
                            {
                                txt_Datecode4Number = lastTwoDigitsOfYear + weekOfYear;
                                txt_Datecode3Number = lastOneDigitsOfYear + weekOfYear;

                                dtBoxNumber = dbBoxNumber.GetData(
                                "Select TOP(1) * from M_BARTENDER_PRINT where FILE_NAME = @file_name and DATEPART(WEEK, CREATE_DATE)= @weekOfYear and FLAG_REPRINT = @flag_reprint",
                                new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) },
                                    { "@lot", txtBoxLot.Text },
                                    {"@weekOfYear",weekOfYear },
                                    { "@flag_reprint", '0' },
                                }
                             );
                            }
                            else
                            {
                                dtBoxNumber = dbBoxNumber.GetData(
                                "Select TOP(1) * from M_BARTENDER_PRINT where FILE_NAME = @file_name and DATE_CODE = @dayYearPart and FLAG_REPRINT = @flag_reprint",
                                new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) },
                                    { "@lot", txtBoxLot.Text },
                                    {"@dayYearPart",dayYearPart },
                                    { "@flag_reprint", '0' },
                                }
                             );
                            }
                            if (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0)
                            {
                                //xu lý txtSerial tang gia tri 1 don vi
                                //txtSerialStartUpdate = GetNextSerial(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                //txtSerialUpdate = GetNextSerial(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(CopiesOfLabel.Text));
                                DBAgent dbserialSample = DBAgent.Instance;
                                DataTable dtserialSample = dbserialSample.GetData(
                                    "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                    new Dictionary<string, object> {
                                        { "@file_name", Path.GetFileName(this._basePath) }
                                    }
                                 );
                                txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                if (txtSerialStartInsert.Count() == 5)
                                {
                                    txtSerialStartUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                    txtSerialUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(CopiesOfLabel.Text));
                                }
                                if (txtSerialStartInsert.Count() == 4)
                                {
                                    txtSerialStartUpdate = V80E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                    txtSerialUpdate = V80E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(CopiesOfLabel.Text));
                                }
                                if (txtSerialStartInsert.Count() == 3)
                                {
                                    txtSerialStartUpdate = V12E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                    txtSerialUpdate = V12E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(CopiesOfLabel.Text));
                                }

                                foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                {
                                    //cac gia tri co the chinh sua
                                    foreach (TemplateField field in barcodeField.Fields)
                                    {
                                        // Load current textbox value 
                                        var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                        //thanh add txtLot
                                        if (field.Name.Contains("txt_Lot"))
                                        {
                                            txtLot = txtBox.Text;

                                            field.BtwValue = txtLot;
                                            this._bartenderEngine.SetValue(field.Name, txtLot);

                                        }
                                    }
                                    //gia tri serial tu dong tang
                                    foreach (TemplateField field in barcodeField.UncheckFields)
                                    {
                                        // Load current textbox value 
                                        var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                        //thanh add txtProduct
                                        if (field.Name.Contains("txt_Product"))
                                        {
                                            txtProduct = txtBox.Text;
                                            field.BtwValue = txtProduct;
                                            this._bartenderEngine.SetValue(field.Name, txtProduct);
                                        }
                                        //thanh add txtSerial
                                        if (field.Name.Contains("txt_Serial"))
                                        {
                                            field.BtwValue = txtSerialStartUpdate;
                                            this._bartenderEngine.SetValue(field.Name, txtSerialStartUpdate);
                                        }

                                        if (field.Name == "txt_Datecode3Number")
                                        {
                                            txtDatecode = txt_Datecode3Number;
                                            field.BtwValue = OneYearTwoWeek(true);
                                            this._bartenderEngine.SetValue(field.Name, OneYearTwoWeek(true));
                                        }
                                        if (field.Name == "txt_Datecode4Number")
                                        {
                                            txtDatecode = txt_Datecode4Number;
                                            field.BtwValue = TwoYearTwoWeek(true);
                                            this._bartenderEngine.SetValue(field.Name, TwoYearTwoWeek(true));
                                        }
                                        if (field.Name == "txt_Datecode")
                                        {
                                            txtDatecode = dayYearPart;
                                            if (checkFileName == "V40W12BS2P5-07A021 NCVH THUNG(2).btw")
                                            {
                                                txtDatecode = formatDate2YMD();
                                            }
                                            field.BtwValue = txtDatecode;
                                            this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                        }

                                    }
                                }
                                flagUpdate = true;
                            }
                            else
                            {
                                DBAgent dbserialSample = DBAgent.Instance;
                                DataTable dtserialSample = dbserialSample.GetData(
                                    "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                    new Dictionary<string, object> {
                                        { "@file_name", Path.GetFileName(this._basePath) }
                                    }
                                 );
                                if (dtserialSample != null && dtserialSample.Rows.Count > 0)
                                {
                                    //xu ly  serial_sequence khong can tang, vi chua in lan nao theo Product & Lot
                                    foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                    {
                                        //cac gia tri co the chinh sua
                                        foreach (TemplateField field in barcodeField.Fields)
                                        {
                                            // Load current textbox value 
                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                            //thanh add txtLot
                                            if (field.Name.Contains("txt_Lot"))
                                            {
                                                txtLot = txtBox.Text;
                                                field.BtwValue = txtLot;
                                                this._bartenderEngine.SetValue(field.Name, txtLot);

                                            }
                                        }
                                        //gia tri serial tu dong tang
                                        foreach (TemplateField field in barcodeField.UncheckFields)
                                        {
                                            // Load current textbox value 
                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                            //thanh add txtProduct
                                            if (field.Name.Contains("txt_Product"))
                                            {
                                                txtProduct = txtBox.Text;
                                                field.BtwValue = txtProduct;
                                                this._bartenderEngine.SetValue(field.Name, txtProduct);
                                            }
                                            if (field.Name == "txt_Datecode3Number")
                                            {
                                                txtDatecode = txt_Datecode3Number;
                                                field.BtwValue = OneYearTwoWeek(true);
                                                this._bartenderEngine.SetValue(field.Name, OneYearTwoWeek(true));
                                            }
                                            if (field.Name == "txt_Datecode4Number")
                                            {
                                                txtDatecode = txt_Datecode4Number;
                                                field.BtwValue = TwoYearTwoWeek(true);
                                                this._bartenderEngine.SetValue(field.Name, TwoYearTwoWeek(true));
                                            }
                                            if (field.Name == "txt_Datecode")
                                            {
                                                txtDatecode = dayYearPart;
                                                if (checkFileName == "V40W12BS2P5-07A021 NCVH THUNG(2).btw")
                                                {
                                                    txtDatecode = formatDate2YMD();
                                                }
                                                field.BtwValue = txtDatecode;
                                                this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                            }
                                            //thanh add txtSerial
                                            if (field.Name.Contains("txt_Serial"))
                                            {
                                                txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                                if (txtSerialStartInsert.Count() == 5)
                                                    //txtSerialInsert = txtBox.Text;
                                                    txtSerialInsert = CV60E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                                else if (txtSerialStartInsert.Count() == 3)
                                                {
                                                    txtSerialInsert = V12E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                                }
                                                else
                                                {
                                                    txtSerialInsert = V80E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                                }
                                                field.BtwValue = txtSerialStartInsert;
                                                this._bartenderEngine.SetValue(field.Name, txtSerialStartInsert);
                                            }
                                        }
                                    }
                                    flagUpdate = false;
                                }
                                else
                                {
                                    System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Lot.\nXem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            if (flagUpdate == true)
                            {
                                try
                                {
                                    //cap nhat du lieu
                                    //int box_number = int.Parse(CopiesOfLabel.Text) - 1;

                                    DBAgent db = DBAgent.Instance;
                                    string query = "UPDATE M_BARTENDER_PRINT SET SERIAL_NUMBER = @box, UPDATE_DATE = @update_date, UPDATE_BY = @update_by WHERE FILE_NAME = @filename AND DATE_CODE = @date_code AND FLAG_REPRINT = @flag_print";
                                    if (checkFileName == "V40W12BS2P5-07A021 NCVH THUNG(2).btw")
                                    {
                                        query = "UPDATE M_BARTENDER_PRINT SET SERIAL_NUMBER = @box, UPDATE_DATE = @update_date, UPDATE_BY = @update_by WHERE FILE_NAME = @filename AND DATEPART(WEEK, CREATE_DATE)=@weekOfYear AND FLAG_REPRINT = @flag_print";
                                    }
                                    Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@box", txtSerialUpdate },
                                            { "@update_date", DateTime.Now },
                                            { "@update_by", Department },
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@weekOfYear", weekOfYear },
                                            { "@date_code", dayYearPart },
                                            { "@flag_print", '0' },
                                        };

                                    db.Execute(query, parameters);
                                }
                                catch
                                {
                                    System.Windows.Forms.MessageBox.Show("Không thể cập nhật dữ liệu!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            else
                            {
                                try
                                {
                                    //them du lieu
                                    int box_number = int.Parse(CopiesOfLabel.Text);
                                    DBAgent db = DBAgent.Instance;
                                    string query = "INSERT INTO M_BARTENDER_PRINT (FILE_NAME, DATE_CODE, SERIAL_NUMBER, FLAG_REPRINT,NUMBER_REPRINT, CREATE_DATE,UPDATE_DATE, CREATE_BY, UPDATE_BY) VALUES (@filename, @date, @box_number, @flag_print,@number_reprint, @create_date,@update_date, @create_by,@update_by)";

                                    Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", dayYearPart },
                                            { "@box_number", txtSerialInsert },
                                            { "@flag_print", '0' },
                                            { "@number_reprint", DBNull.Value },
                                            { "@create_date", DateTime.Now },
                                            { "@update_date",DBNull.Value },
                                            { "@create_by", Department },
                                            { "@update_by", DBNull.Value }

                                        };
                                    db.Execute(query, parameters);

                                }
                                catch
                                {
                                    System.Windows.Forms.MessageBox.Show("Không thể thêm dữ liệu mới!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            if (checkFileName != "V40W12BS2P5-07A021 NCVH UKCA(3).btw")
                            {
                                DBAgent db_ = DBAgent.Instance;
                                int numberlabel = int.Parse(CopiesOfLabel.Text);
                                string query_bartender_history = "INSERT INTO M_BAR_HISTORY (FILE_NAME, DATE_CODE, LOT_NO, GOOGLE_NUMBER,STR_SERIAL,END_SERIAL,CREATE_DATE, CREATE_BY,NUMBER_LABEL, STATUS) VALUES (@filename, @date, @lot_no, @google_number,@str_serial, @end_serial,@create_date, @create_by,@number_label,@status)";
                                Dictionary<string, object> parameters_bartender_history = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", dayYearPart },
                                            { "@lot_no", txtLot },
                                            { "@google_number", txtGoogleNumber==null?"": txtGoogleNumber.Text },
                                            { "@str_serial", txt_Serial.Text },
                                            { "@end_serial", txtSerialInsert==""?txtSerialUpdate: txtSerialInsert},
                                            { "@create_date", DateTime.Now },
                                            { "@create_by", Department },
                                            { "@number_label", numberlabel },
                                            { "@status", "NORMAL-PRINT" },
                                        };
                                db_.Execute(query_bartender_history, parameters_bartender_history);
                            }


                            //thanh add Lien ket voi BarTender de in
                            PrintJob printJob = new PrintJob();
                            printJob.Serializiers = int.Parse(CopiesOfLabel.Text);
                            int check = int.Parse(CopiesOfLabel.Text);
                            printJob.CopiesOfLabel = 1;
                            if (checkFileName == "V40W12BS2P5-07A021 NCVH UKCA(3).btw")
                            {
                                printJob.CopiesOfLabel = int.Parse(CopiesOfLabel.Text);
                                printJob.Serializiers = 1;
                            }
                            else
                            {
                                printJob.CopiesOfLabel = 1;
                            }
                            printJob.Path = this._basePath;
                            printJob.Printer = this.ComboBoxPrintersList.Text;
                            Seagull.BarTender.Print.Result result = this._bartenderEngine.Print(printJob);
                            if (result == Seagull.BarTender.Print.Result.Success)
                            {
                                LoadBtwData();
                                ClearStackPanel();
                                RenderUIUsingBtwData();
                                System.Windows.Forms.MessageBox.Show("In thành công!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                System.Windows.Forms.MessageBox.Show("In thất bại!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Vui lòng chọn máy in!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Product.\nXem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Không tải được dữ liệu!" + ex, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Vui lòng chọn file!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void RePrint(string username)
        {
            RePrint rePrint = new RePrint();
            if (rePrint.ShowDialog() == true)
            {
                if (rePrint.DialogResult == true)
                {
                    try
                    {
                        string startNumber = rePrint.txtStartNumber.Text;
                        string lotNoRePrint = rePrint.txtLotNo.Text;
                        int numberLabel = Int16.Parse(rePrint.txtNumberLabel.Text);

                        //thuc hien chuc nang in lai 
                        if (this._basePath != null)
                        {
                            try
                            {
                                this._bartenderEngine.OpenFormat(this._basePath);
                                string txtLot = "";
                                string txtProduct = "";
                                string txtDatecode = "";
                                string txtSerial = "";

                                //chọn may in
                                if (this.ComboBoxPrintersList.Text == "" || this.ComboBoxPrintersList.Text == null)
                                {
                                    System.Windows.Forms.MessageBox.Show("Vui lòng chọn máy in!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                var txtBoxLot = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Lot");
                                var txtDate = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Datecode");
                                var txtGoogleNumber = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_GoogleNumber");
                                var txt_Serial = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Serial");
                                DBAgent dbserialSample = DBAgent.Instance;
                                string txtGetProductDay = "";
                                DataTable dtserialSample = dbserialSample.GetData(
                                    "Select TOP(1) * from T_LOT_PRODUCT where LOT_NO = @lot_no",
                                    new Dictionary<string, object> {
                                        { "@lot_no", lotNoRePrint }
                                    }
                                 );
                                if (dtserialSample != null && dtserialSample.Rows.Count > 0)
                                {
                                    txtGetProductDay = dtserialSample.Rows[0]["PRODUCT_DAY"].ToString().Trim();
                                }

                                //thanh add check Model và Lot có ở T_LOT_PRODUCT
                                if (CheckProductionRePrint(Path.GetFileName(this._basePath), lotNoRePrint))
                                {
                                    // lay thong tin tren man hinh
                                    foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                    {
                                        //cac gia tri co the chinh sua
                                        foreach (TemplateField field in barcodeField.Fields)
                                        {
                                            // Load current textbox value 
                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                            //thanh add txtLot
                                            if (field.Name.Contains("txt_Lot"))
                                            {
                                                txtLot = lotNoRePrint;
                                                field.BtwValue = txtLot;
                                                this._bartenderEngine.SetValue(field.Name, txtLot);
                                            }

                                            //thanh add txtDatecode

                                        }
                                        //gia tri serial tu dong tang
                                        foreach (TemplateField field in barcodeField.UncheckFields)
                                        {
                                            // Load current textbox value 
                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                            //thanh add txtSerial
                                            if (field.Name.Contains("txt_Serial"))
                                            {
                                                txtSerial = startNumber.ToString();
                                                field.BtwValue = txtSerial;
                                                this._bartenderEngine.SetValue(field.Name, txtSerial);
                                            }
                                            //thanh add txtProduct
                                            if (field.Name.Contains("txt_Product"))
                                            {
                                                txtProduct = txtBox.Text;
                                                field.BtwValue = txtProduct;
                                                this._bartenderEngine.SetValue(field.Name, txtProduct);
                                            }
                                            if (field.Name.Contains("txt_Datecode"))
                                            {
                                                txtDatecode = Result_FormatDate(txtGetProductDay);
                                                field.BtwValue = txtDatecode;
                                                this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                            }
                                        }
                                    }

                                    //them du lieu in lai
                                    try
                                    {

                                        //them du lieu
                                        DBAgent getdbserialSample = DBAgent.Instance;
                                        DataTable getdtserialSample1 = getdbserialSample.GetData(
                                            "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                            new Dictionary<string, object> {
                                        { "@file_name", Path.GetFileName(this._basePath) }
                                            }
                                         );
                                        string txtSerialUpdate = "";
                                        string txtSerialStartInsert = getdtserialSample1.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                        if (txtSerialStartInsert.Count() == 5)
                                        {
                                            txtSerialUpdate = CV60E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                        }
                                        else if (txtSerialStartInsert.Count() == 3)
                                        {
                                            txtSerialUpdate = V12E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                        }
                                        else
                                        {
                                            txtSerialUpdate = V80E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                        }
                                        int box_number = int.Parse(CopiesOfLabel.Text);


                                        DBAgent db = DBAgent.Instance;
                                        string query = "INSERT INTO M_BARTENDER_PRINT (FILE_NAME, DATE_CODE, SERIAL_NUMBER, FLAG_REPRINT,NUMBER_REPRINT, CREATE_DATE,UPDATE_DATE, CREATE_BY, UPDATE_BY) VALUES (@filename, @date, @box_number, @flag_print,@number_reprint, @create_date,@update_date, @create_by,@update_by)";

                                        Dictionary<string, object> parameters = new Dictionary<string, object> {
                                                    { "@filename", Path.GetFileName(this._basePath) },
                                                    { "@lotno", txtLot },
                                                    { "@date", Result_FormatDate(txtGetProductDay) },
                                                    { "@box_number", startNumber },
                                                    { "@flag_print", '1' },
                                                    { "@number_reprint", numberLabel },
                                                    { "@create_date", DateTime.Now },
                                                    { "@update_date", DBNull.Value },
                                                    { "@create_by", username },
                                                    { "@update_by", DBNull.Value }
                                                };
                                        db.Execute(query, parameters);
                                        DBAgent db_ = DBAgent.Instance;
                                        int numberlabel = int.Parse(CopiesOfLabel.Text);
                                        string query_bartender_history = "INSERT INTO M_BAR_HISTORY (FILE_NAME, DATE_CODE, LOT_NO, GOOGLE_NUMBER,STR_SERIAL,END_SERIAL,CREATE_DATE, CREATE_BY,NUMBER_LABEL,STATUS) VALUES (@filename, @date, @lot_no, @google_number,@str_serial, @end_serial,@create_date, @create_by,@number_label,@status)";
                                        Dictionary<string, object> parameters_bartender_history = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", Result_FormatDate(txtGetProductDay) },
                                            { "@lot_no", txtLot },
                                            { "@google_number", GoogleNumber  },
                                            { "@str_serial", startNumber },
                                            { "@end_serial", txtSerialUpdate},
                                            { "@create_date", DateTime.Now },
                                            { "@create_by", Department },
                                            { "@number_label", numberLabel },
                                            { "@status", "RE_PRINT" },

                                        };

                                        db_.Execute(query_bartender_history, parameters_bartender_history);
                                    }
                                    catch
                                    {
                                        System.Windows.Forms.MessageBox.Show("Không thể thêm dữ liệu mới!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }


                                    //thanh add Lien ket voi BarTender de in
                                    PrintJob printJob = new PrintJob();
                                    printJob.Serializiers = numberLabel;
                                    printJob.CopiesOfLabel = 1;
                                    printJob.Path = this._basePath;
                                    printJob.Printer = this.ComboBoxPrintersList.Text;
                                    Seagull.BarTender.Print.Result result = this._bartenderEngine.Print(printJob);
                                    if (result == Seagull.BarTender.Print.Result.Success)
                                    {
                                        System.Windows.Forms.MessageBox.Show("In thành công!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("In thất bại!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    }
                                }
                                else
                                {
                                    System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Product. Xem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Windows.Forms.MessageBox.Show("Có lỗi xảy ra!" + ex, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Vui lòng chọn file!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Vui lòng nhập kiểu số!");
                    }
                }
            }
        }
        private bool CheckProduction(string filename, string lotno)
        {
            try
            {
                DBAgent dbProduct = DBAgent.Instance;
                DataTable dtProduct = new DataTable();
                DateTime TimeNow = DateTime.Now;
                DateTime checkTimeProductEnd = DateTime.Now;


                string formattedDate = TimeNow.ToString("yyyyMMdd");
                DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
                if (TimeNow < sixThirtyAM)
                {
                    TimeNow = TimeNow.AddDays(-1);
                    formattedDate = TimeNow.ToString("yyyyMMdd");
                }
                string query = @"
                    SELECT A.PRODUCT_NO
                    FROM T_LOT_PRODUCT AS A
                    INNER JOIN M_BARTENDER AS B ON A.PRODUCT_NO = B.PRODUCT_NO
                    WHERE B.FILENAME = @filename AND A.LOT_NO = @lotno AND  A.PRODUCT_END_DT>=@checkTime AND PRODUCT_DAY = @formattedDate";
                Dictionary<string, object> parameters = new Dictionary<string, object> {
                    { "@filename", filename },
                    { "@lotno", lotno },
                    { "@checkTime", checkTimeProductEnd },
                    { "@formattedDate", formattedDate }
                };
                dtProduct = dbProduct.GetData(query, parameters);
                if (dtProduct != null && dtProduct.Rows.Count > 0 || filename == "V40W12BS2P5-07A021 NCVH UKCA(3).btw")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Lỗi truy vấn dữ liệu sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return false;
        }
        private bool CheckProductionRePrint(string filename, string lotno)
        {
            try
            {
                DBAgent dbProduct = DBAgent.Instance;
                DataTable dtProduct = new DataTable();
                DateTime dateTimeRePrint = DateTime.Now;
                string query = @"
                    SELECT A.PRODUCT_NO
                    FROM T_LOT_PRODUCT AS A
                    INNER JOIN M_BARTENDER AS B ON A.PRODUCT_NO = B.PRODUCT_NO
                    WHERE B.FILENAME = @filename AND A.LOT_NO = @lotno";

                Dictionary<string, object> parameters = new Dictionary<string, object> {
                    { "@filename", filename },
                    { "@lotno", lotno },
                   // { "@dateTimeRePrint", dateTimeRePrint }
                };

                dtProduct = dbProduct.GetData(query, parameters);
                if (dtProduct != null && dtProduct.Rows.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Lỗi truy vấn dữ liệu sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return false;
        }
        private bool CheckProductionInventory(string filename, string lotno)
        {
            try
            {
                DBAgent dbProduct = DBAgent.Instance;
                DataTable dtProduct = new DataTable();
                DateTime CheckProductionInventory = DateTime.Now;
                string product_date_Inventory = CheckProductionInventory.ToString("yyyyMMdd");
                string query = @"
                    SELECT A.PRODUCT_NO
                    FROM T_LOT_PRODUCT AS A
                    INNER JOIN M_BARTENDER AS B ON A.PRODUCT_NO = B.PRODUCT_NO
                    WHERE B.FILENAME = @filename AND A.LOT_NO = @lotno AND SHIFT_CD =@shift_cd AND PRODUCT_END_DT >= @checkdate ";
                Dictionary<string, object> parameters = new Dictionary<string, object> {
                    { "@filename", filename },
                    { "@lotno", lotno },
                    { "@shift_cd", "CA1" },
                    { "@checkdate",CheckProductionInventory  }
                };

                dtProduct = dbProduct.GetData(query, parameters);
                if (dtProduct != null && dtProduct.Rows.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Lỗi truy vấn dữ liệu sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return false;
        }

        private void Button_Print_Click(object sender, RoutedEventArgs e)
        {
            this.Print();
        }

        private void Button_Open_File_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = this._basePath;
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
            }
            catch (Exception ex)
            {
            }
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.Refresh_Form();
        }

        private void Refresh_Form()
        {
            this.Refresh_Button.IsEnabled = false;
            this._bartenderEngine.CloseFormat(false);
            this.ClearStackPanel();
            new Thread(this.Loading).Start();
        }
        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            this._bartenderEngine.Dispose();
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string changedValue)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(changedValue));
            }
        }
        private IList<string> GetInstalledPrinters()
        {
            IList<string> prt = new List<string>();
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                prt.Add(p);
            }

            return prt;
        }
        private void ClearStackPanel()
        {
            if (checkFieldStackPanel.Children.Count == 0)
            {
                return;
            }
            foreach (BarcodeField barcodeField in this._btwData.Barcodes)
            {
                foreach (TemplateField field in barcodeField.Fields)
                {
                    var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                    txtBox.UnregisterName(txtBox.Name);
                    // this.someStackPanel.UnregisterName(txtBox.Name);
                }
            }

            this.checkFieldStackPanel.Children.Clear();

            if (unCheckFieldStackPanel.Children.Count == 0)
            {
                return;
            }
            foreach (BarcodeField barcodeField in this._btwData.Barcodes)
            {
                foreach (TemplateField field in barcodeField.UncheckFields)
                {
                    var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                    txtBox.UnregisterName(txtBox.Name);
                    // this.someStackPanel.UnregisterName(txtBox.Name);
                }
            }

            this.unCheckFieldStackPanel.Children.Clear();
        }

        private void Loading()
        {
            this.LoadBtwData();
            this.RenderUIUsingBtwData();
        }
        private void Button_Reload(object sender, RoutedEventArgs e)
        {
            LoadFilePath();
            //Refresh_Form();
            this.Refresh_Button.IsEnabled = false;
            this._bartenderEngine.CloseFormat(false);
            this.ClearStackPanel();
        }
        private void Button_Sample_Print(object sender, RoutedEventArgs e)
        {
            if (this._basePath != null)
            {
                try
                {
                    //thanh kiem tra thong tin so luong nhan co phai la interger

                    this._bartenderEngine.OpenFormat(this._basePath);
                    //chọn may in
                    if (this.ComboBoxPrintersList.Text != "" && this.ComboBoxPrintersList.Text != null)
                    {


                        foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                        {
                            //cac gia tri co the chinh sua
                            foreach (TemplateField field in barcodeField.Fields)
                            {
                                // Load current textbox value 
                                var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                //thanh add txtLot
                                if (field.Name.Contains("txt_Lot"))
                                {
                                    string txtLot = txtBox.Text;
                                    field.BtwValue = txtLot;
                                    this._bartenderEngine.SetValue(field.Name, txtLot);
                                }

                                //thanh add txtDatecode
                                if (field.Name.Contains("txt_Datecode"))
                                {
                                    string txtDatecode = txtBox.Text;
                                    field.BtwValue = txtDatecode;
                                    this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                }
                            }
                            //gia tri serial tu dong tang
                            foreach (TemplateField field in barcodeField.UncheckFields)
                            {
                                // Load current textbox value 
                                var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                //thanh add txtProduct
                                if (field.Name.Contains("txt_Product"))
                                {
                                    string txtProduct = txtBox.Text;
                                    field.BtwValue = txtProduct;
                                    this._bartenderEngine.SetValue(field.Name, txtProduct);
                                }
                                //thanh add txtSerial
                                if (field.Name.Contains("txt_Serial"))
                                {
                                    string txtSerial = txtBox.Text;
                                    field.BtwValue = txtSerial;
                                    this._bartenderEngine.SetValue(field.Name, txtSerial);
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Vui lòng chọn máy in!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //thanh add Lien ket voi BarTender de in
                    PrintJob printJob = new PrintJob();
                    printJob.Serializiers = 1;
                    printJob.CopiesOfLabel = 1;
                    printJob.Path = this._basePath;
                    printJob.Printer = this.ComboBoxPrintersList.Text;
                    Seagull.BarTender.Print.Result result = this._bartenderEngine.Print(printJob);
                    if (result == Seagull.BarTender.Print.Result.Success)
                    {
                        System.Windows.Forms.MessageBox.Show("In thành công!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("In thất bại!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }

                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Lỗi in thử\n Vui lòng thử lại!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        public string Result_FormatDate(string dateRePrint = null)
        {
            DBAgent dbBoxNumber = DBAgent.Instance;
            DBAgent dbBoxNumberStart = DBAgent.Instance;
            DataTable dtBoxNumberStart = dbBoxNumberStart.GetData(
                               "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                               new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) }
                               }
                            );

            dayYearPart = dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim().Length == 5 ? formatDateToRePrint(dateRePrint) : formatDate4serial(DateTime.Now);

            return dayYearPart;
        }
        private void Button_Eventory_Print(object sender, RoutedEventArgs e)
        {
            PrintInventory PrintInventory = new PrintInventory();
            DateTime currentDateTime = DateTime.Now;
            var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(directory + "\\" + CONFIG_FILE);
            string startTime = data["Database"]["StartTimeInventory"];
            int startTimeInventory = int.Parse(startTime);
            string endTime = data["Database"]["EndTimeInventory"];
            int endTimeInventory = int.Parse(endTime);

            // Kiểm tra nếu thời gian hiện tại nằm trong khoảng từ 19h ngày hôm trước đến 7h ngày hôm sau
            DateTime TimeNow = DateTime.Now;
            DateTime sixThirtyAM = new DateTime(TimeNow.Year, TimeNow.Month, TimeNow.Day, 6, 30, 0);
            if (TimeNow > sixThirtyAM)
            {
                System.Windows.Forms.MessageBox.Show("Chưa đến thời gian cho phép in tồn!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            isBCalled = true;
            DBAgent dbBoxNumber = DBAgent.Instance;
            DBAgent dbBoxNumberStart = DBAgent.Instance;
            DataTable dtBoxNumberStart = dbBoxNumberStart.GetData(
                               "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                               new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) }
                               }
                            );

            dayYearPart = dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim().Length == 5 ? formatDateToInventory(DateTime.Now) : FormatDate4serialInventory(DateTime.Now);

            DataTable dtBoxNumber = dbBoxNumber.GetData(
                                "Select TOP(1) * from M_BARTENDER_PRINT where FILE_NAME = @file_name and DATE_CODE = @date and FLAG_REPRINT = @flag_reprint",
                                new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) },
                                    {"@date",dayYearPart },
                                    { "@flag_reprint", '0' },
                                }
                             );


            PrintInventory.txtStartNumber.Text = (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0)
             ? dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim()
             : dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();

            PrintInventory.txtDateCode.Text = dayYearPart;
            bool flagUpdate = false;
            flagUpdate = (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0) ? false : true;
            if (PrintInventory.ShowDialog() == true)
            {
                string startNumber = PrintInventory.txtStartNumber.Text;
                string dateCodePrintInventory = PrintInventory.txtDateCode.Text;
                string txtLotNo = PrintInventory.txtLotNoInventory.Text;
                string txtNumberLabel = PrintInventory.txtNumberLabel.Text;
                DBAgent dbNumberLabel = DBAgent.Instance;
                DataTable dtNumberLabel = dbNumberLabel.GetData(
                    "SELECT SUM(NUMBER_LABEL) AS TotalNumberLabel FROM M_BAR_HISTORY WHERE STATUS = @INVENTORY_PRINT and LOT_NO = @LOT_NO",
                    new Dictionary<string, object> {
                        { "@INVENTORY_PRINT", "INVENTORY_PRINT" },
                        { "@LOT_NO",  txtLotNo},
                    }
                );

                if (dtNumberLabel != null && dtNumberLabel.Rows.Count > 0)
                {
                    string numberSerial = dtNumberLabel.Rows[0]["TotalNumberLabel"].ToString().Trim();
                    if (numberSerial != "")
                    {
                        int tong = int.Parse(numberSerial) + int.Parse(txtNumberLabel);
                        if (tong > 2000)
                        {
                            System.Windows.Forms.MessageBox.Show($"Số lượng nhãn còn lại có thể in tồn là: {2000 - int.Parse(numberSerial)}! Vui lòng nhập lại", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                    }
                }
                string txtSerialStartUpdate = "";
                string txtSerialUpdate = "";
                string txtSerialInsert = "";
                if (PrintInventory.DialogResult == true)
                {
                    try
                    {
                        int numberLabel = Int16.Parse(PrintInventory.txtNumberLabel.Text);

                        DateTime currentTime = DateTime.Now;

                        // Nếu là 4 giờ sáng, reset giá trị Count


                        if (Properties.Settings.Default.FlagCheck == true)
                        {
                            Properties.Settings.Default.FlagCheck = false;
                            Properties.Settings.Default.Count = 0;
                            Properties.Settings.Default.Save();
                        }
                        Properties.Settings.Default.Count += numberLabel;
                        Properties.Settings.Default.Save();
                        if (numberLabel >= 1001)
                        {
                            System.Windows.Forms.MessageBox.Show("Nhập vượt quá số lượng cần in tồn!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        //thuc hien chuc nang in lai 
                        if (this._basePath != null)
                        {
                            try
                            {
                                this._bartenderEngine.OpenFormat(this._basePath);
                                string txtLot = "";
                                string txtProduct = "";
                                string txtDatecode = "";
                                string txtSerial = "";

                                //chọn may in
                                if (this.ComboBoxPrintersList.Text == "" || this.ComboBoxPrintersList.Text == null)
                                {
                                    System.Windows.Forms.MessageBox.Show("Vui lòng chọn máy in!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                var txtBoxLot = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Lot");

                                //thanh add check Model và Lot có ở T_LOT_PRODUCT
                                if (CheckProductionInventory(Path.GetFileName(this._basePath), txtLotNo))
                                {
                                    // lay thong tin tren man hinh
                                    foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                    {
                                        foreach (TemplateField field in barcodeField.Fields)
                                        {
                                            // Load current textbox value 
                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                            //thanh add txtLot
                                            if (field.Name.Contains("txt_Lot"))
                                            {
                                                txtLot = txtLotNo;
                                                field.BtwValue = txtLot;
                                                this._bartenderEngine.SetValue(field.Name, txtLot);
                                            }
                                        }
                                        //gia tri serial tu dong tang
                                        foreach (TemplateField field in barcodeField.UncheckFields)
                                        {
                                            // Load current textbox value 
                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                            //thanh add txtSerial
                                            if (field.Name.Contains("txt_Serial"))
                                            {
                                                txtSerial = startNumber.ToString();
                                                this._bartenderEngine.SetValue(field.Name, txtSerial);
                                            }
                                            //thanh add txtProduct
                                            if (field.Name.Contains("txt_Product"))
                                            {
                                                txtProduct = txtBox.Text;
                                                field.BtwValue = txtProduct;
                                                this._bartenderEngine.SetValue(field.Name, txtProduct);
                                            }
                                            if (field.Name.Contains("txt_Datecode"))
                                            {
                                                //txtDatecode = dateCodePrintInventory;
                                                txtDatecode = formatDate2YMDInventory();
                                                field.BtwValue = dateCodePrintInventory;
                                                this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                            }
                                        }
                                    }

                                    //them du lieu in lai
                                    if (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0)
                                    {
                                        DBAgent dbserialSample = DBAgent.Instance;
                                        DataTable dtserialSample = dbserialSample.GetData(
                                            "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                            new Dictionary<string, object> {
                                                 { "@file_name", Path.GetFileName(this._basePath) }
                                            }
                                         );
                                        string txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                        if (txtSerialStartInsert.Count() == 5)
                                        {
                                            txtSerialStartUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                            txtSerialUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(txtNumberLabel));
                                        }
                                        if (txtSerialStartInsert.Count() == 4)
                                        {
                                            txtSerialStartUpdate = V80E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                            txtSerialUpdate = V80E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(txtNumberLabel));
                                        }
                                        if (txtSerialStartInsert.Count() == 3)
                                        {
                                            txtSerialStartUpdate = V12E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                            txtSerialUpdate = V12E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(txtNumberLabel));
                                        }

                                        foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                        {
                                            //cac gia tri co the chinh sua
                                            foreach (TemplateField field in barcodeField.Fields)
                                            {
                                                // Load current textbox value 
                                                var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                //thanh add txtLot
                                                if (field.Name.Contains("txt_Lot"))
                                                {
                                                    txtLot = txtLotNo;
                                                    field.BtwValue = txtLot;
                                                    this._bartenderEngine.SetValue(field.Name, txtLot);
                                                }
                                            }
                                            //gia tri serial tu dong tang
                                            foreach (TemplateField field in barcodeField.UncheckFields)
                                            {
                                                var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                //thanh add txtProduct
                                                if (field.Name.Contains("txt_Product"))
                                                {
                                                    txtProduct = txtBox.Text;
                                                    field.BtwValue = txtProduct;
                                                    this._bartenderEngine.SetValue(field.Name, txtProduct);
                                                }
                                                //thanh add txtSerial
                                                if (field.Name.Contains("txt_Serial"))
                                                {
                                                    field.BtwValue = txtSerialStartUpdate;
                                                    this._bartenderEngine.SetValue(field.Name, txtSerialStartUpdate);
                                                }
                                                if (field.Name == "txt_Datecode3Number")
                                                {
                                                    txtDatecode = txt_Datecode3Number;
                                                    field.BtwValue = OneYearTwoWeek(false);
                                                    this._bartenderEngine.SetValue(field.Name, OneYearTwoWeek(false));
                                                }
                                                if (field.Name == "txt_Datecode4Number")
                                                {
                                                    txtDatecode = txt_Datecode4Number;
                                                    field.BtwValue = TwoYearTwoWeek(false);
                                                    this._bartenderEngine.SetValue(field.Name, TwoYearTwoWeek(false));
                                                }
                                                if (field.Name == "txt_Datecode")
                                                {
                                                    txtDatecode = dayYearPart;
                                                    txtDatecode = formatDate2YMDInventory();
                                                    field.BtwValue = txtDatecode;
                                                    this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                }
                                            }
                                        }
                                        flagUpdate = true;
                                    }
                                    else
                                    {
                                        DBAgent dbserialSample = DBAgent.Instance;
                                        DataTable dtserialSample = dbserialSample.GetData(
                                            "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                            new Dictionary<string, object> {
                                        { "@file_name", Path.GetFileName(this._basePath) }
                                            }
                                         );
                                        if (dtserialSample != null && dtserialSample.Rows.Count > 0)
                                        {
                                            //xu ly  serial_sequence khong can tang, vi chua in lan nao theo Product & Lot
                                            foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                            {
                                                //cac gia tri co the chinh sua
                                                foreach (TemplateField field in barcodeField.Fields)
                                                {
                                                    // Load current textbox value 
                                                    var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                    //thanh add txtLot
                                                    if (field.Name.Contains("txt_Lot"))
                                                    {
                                                        txtLot = txtLotNo;
                                                        field.BtwValue = txtLot;
                                                        this._bartenderEngine.SetValue(field.Name, txtLot);
                                                    }

                                                    //thanh add txtDatecode
                                                    if (field.Name.Contains("txt_Datecode"))
                                                    {
                                                        txtDatecode = txtBox.Text;
                                                        field.BtwValue = txtDatecode;
                                                        this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                    }
                                                }
                                                //gia tri serial tu dong tang
                                                foreach (TemplateField field in barcodeField.UncheckFields)
                                                {
                                                    // Load current textbox value 
                                                    var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                    //thanh add txtProduct
                                                    if (field.Name.Contains("txt_Product"))
                                                    {
                                                        txtProduct = txtBox.Text;
                                                        field.BtwValue = txtProduct;
                                                        this._bartenderEngine.SetValue(field.Name, txtProduct);
                                                    }
                                                    if (field.Name == "txt_Datecode3Number")
                                                    {
                                                        txtDatecode = txt_Datecode3Number;
                                                        field.BtwValue = OneYearTwoWeek(false);
                                                        this._bartenderEngine.SetValue(field.Name, OneYearTwoWeek(false));
                                                    }
                                                    if (field.Name == "txt_Datecode4Number")
                                                    {
                                                        txtDatecode = txt_Datecode4Number;
                                                        field.BtwValue = TwoYearTwoWeek(false);
                                                        this._bartenderEngine.SetValue(field.Name, TwoYearTwoWeek(false));
                                                    }
                                                    if (field.Name == "txt_Datecode")
                                                    {
                                                        txtDatecode = dayYearPart;
                                                        txtDatecode = formatDate2YMDInventory();
                                                        field.BtwValue = txtDatecode;
                                                        this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                    }
                                                    //thanh add txtSerial

                                                    if (field.Name.Contains("txt_Serial"))
                                                    {

                                                        string txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                                        if (txtSerialStartInsert.Count() == 5)
                                                            //txtSerialInsert = txtBox.Text;
                                                            txtSerialInsert = CV60E(txtSerialStartInsert, int.Parse(txtNumberLabel) - 1);

                                                        else if (txtSerialStartInsert.Count() == 3)
                                                        {
                                                            txtSerialInsert = V12E(txtSerialStartInsert, int.Parse(txtNumberLabel) - 1);
                                                        }
                                                        else
                                                        {
                                                            txtSerialInsert = V80E(txtSerialStartInsert, int.Parse(txtNumberLabel) - 1);

                                                        }
                                                        field.BtwValue = txtSerialStartInsert;
                                                        this._bartenderEngine.SetValue(field.Name, txtSerialStartInsert);
                                                    }
                                                }
                                            }
                                            flagUpdate = false;
                                        }
                                        else
                                        {
                                            System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Lot.\nXem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                    if (flagUpdate == true)
                                    {
                                        try
                                        {
                                            //cap nhat du lieu
                                            //int box_number = int.Parse(CopiesOfLabel.Text) - 1;
                                            DBAgent db = DBAgent.Instance;
                                            string query = "UPDATE M_BARTENDER_PRINT SET SERIAL_NUMBER = @SERIAL_NUMBER, UPDATE_DATE = @update_date, UPDATE_BY = @update_by WHERE FILE_NAME = @filename AND DATE_CODE = @lot_no AND FLAG_REPRINT = @flag_print";
                                            Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@SERIAL_NUMBER", txtSerialUpdate },
                                            { "@update_date", DateTime.Now },
                                            { "@update_by", Department },
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@lot_no", dayYearPart },
                                            { "@flag_print", '0' },
                                        };

                                            db.Execute(query, parameters);
                                        }
                                        catch
                                        {
                                            System.Windows.Forms.MessageBox.Show("Không thể cập nhật dữ liệu!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            //them du lieu
                                            int box_number = int.Parse(CopiesOfLabel.Text);
                                            DBAgent db = DBAgent.Instance;
                                            string query = "INSERT INTO M_BARTENDER_PRINT (FILE_NAME, DATE_CODE, SERIAL_NUMBER, FLAG_REPRINT,NUMBER_REPRINT, CREATE_DATE,UPDATE_DATE, CREATE_BY, UPDATE_BY) VALUES (@filename, @date, @box_number, @flag_print,@number_reprint, @create_date,@update_date, @create_by,@update_by)";

                                            Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", dayYearPart },
                                            { "@box_number", txtSerialInsert },
                                            { "@flag_print", '0' },
                                            { "@number_reprint", DBNull.Value },
                                            { "@create_date", DateTime.Now },
                                            { "@update_date",DBNull.Value },
                                            { "@create_by", Department },
                                            { "@update_by", DBNull.Value }

                                        };
                                            db.Execute(query, parameters);
                                        }
                                        catch
                                        {
                                            System.Windows.Forms.MessageBox.Show("Không thể thêm dữ liệu mới!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                    DBAgent db_ = DBAgent.Instance;
                                    int numberlabel = int.Parse(CopiesOfLabel.Text);
                                    string resultNotNull = null;

                                    if (!string.IsNullOrEmpty(txtSerialUpdate))
                                    {
                                        resultNotNull = txtSerialUpdate;
                                    }
                                    else if (!string.IsNullOrEmpty(txtSerialInsert))
                                    {
                                        resultNotNull = txtSerialInsert;
                                    }


                                    string query_bartender_history = "INSERT INTO M_BAR_HISTORY (FILE_NAME, DATE_CODE, LOT_NO, GOOGLE_NUMBER,STR_SERIAL,END_SERIAL,CREATE_DATE, CREATE_BY,NUMBER_LABEL,STATUS) VALUES (@filename, @date, @lot_no, @google_number,@str_serial, @end_serial,@create_date, @create_by,@number_label,@status)";
                                    Dictionary<string, object> parameters_bartender_history = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", dayYearPart },
                                            { "@lot_no", txtLot },
                                            { "@google_number", GoogleNumber == null?"": GoogleNumber},
                                            { "@str_serial", startNumber },
                                            { "@end_serial", resultNotNull},
                                            { "@create_date", DateTime.Now },
                                            { "@create_by", Department },
                                            { "@number_label", numberLabel },
                                            { "@status", "INVENTORY_PRINT" },
                                        };

                                    db_.Execute(query_bartender_history, parameters_bartender_history);
                                    PrintJob printJob = new PrintJob();
                                    printJob.Serializiers = numberLabel;
                                    printJob.CopiesOfLabel = 1;
                                    printJob.Path = this._basePath;
                                    printJob.Printer = this.ComboBoxPrintersList.Text;
                                    Seagull.BarTender.Print.Result result = this._bartenderEngine.Print(printJob);
                                    if (result == Seagull.BarTender.Print.Result.Success)
                                    {
                                        System.Windows.Forms.MessageBox.Show("In thành công!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("In thất bại!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }

                                else
                                {
                                    System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Product. Xem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Windows.Forms.MessageBox.Show("Có lỗi xảy ra!" + ex, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Vui lòng chọn file!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Vui lòng nhập kiểu số!");
                    }
                }
            }

        }
        private string CheckLotNo(string LotNo, string ProductNo)
        {
            try
            {
                DBAgent db = DBAgent.Instance;
                object CheckLotNo = db.GetValue("SELECT PRODUCT_DAY FROM T_LOT_PRODUCT WHERE LOT_NO = @LotNo AND PRODUCT_NO = @Product_No ",
                                        new Dictionary<string, object> { { "@LotNo", LotNo }, { "@Product_No", ProductNo } });
                string respone = CheckLotNo.ToString();
                return respone;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private void Button_Inventory_Holiday_Click(object sender, RoutedEventArgs e)
        {
            Check_LotNo check_LotNo = new Check_LotNo();
            DBAgent dbBoxNumberStart1 = DBAgent.Instance;
            DataTable dtBoxNumberStart1 = dbBoxNumberStart1.GetData(
                               "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                               new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) }
                               }
                            );

            check_LotNo.txtCheckProductNo.Text = dtBoxNumberStart1.Rows[0]["PRODUCT_NO"].ToString().Trim();
            if (check_LotNo.ShowDialog() == true)
            {
                if (check_LotNo.DialogResult == true)
                {
                    string getProduct_Day = CheckLotNo(check_LotNo.txtCheckLotNo.Text, check_LotNo.txtCheckProductNo.Text);
                    if (getProduct_Day == null)
                    {
                        System.Windows.Forms.MessageBox.Show("LotNo và ProductNo không khớp vui lòng nhập lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    Print_Inventory_Holiday print_Inventory_Holiday = new Print_Inventory_Holiday();
                    var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(directory + "\\" + CONFIG_FILE);
                    string startTime = data["Database"]["StartTimeInventory"];
                    string endTime = data["Database"]["EndTimeInventory"];
                    isBCalled = true;
                    DBAgent dbBoxNumber = DBAgent.Instance;
                    DBAgent dbBoxNumberStart = DBAgent.Instance;
                    DataTable dtBoxNumberStart = dbBoxNumberStart.GetData(
                                       "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                       new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) }
                                       }
                                    );

                    DateTime parsedDate;
                    DateTime.TryParseExact(getProduct_Day, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
                    DateTime checkDateInventory = DateTime.Now.AddDays(1);
                    if (parsedDate <= checkDateInventory)
                    {
                        System.Windows.Forms.MessageBox.Show("LotNo nằm ngoài thời gian in tồn!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    dayYearPart = dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim().Length == 5 ? formatDateToInventory(parsedDate) : formatDate4serial(parsedDate);
                    DataTable dtBoxNumber = dbBoxNumber.GetData(
                                        "Select TOP(1) * from M_BARTENDER_PRINT where FILE_NAME = @file_name and DATE_CODE = @date and FLAG_REPRINT = @flag_reprint",
                                        new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) },
                                    {"@date",dayYearPart },
                                    { "@flag_reprint", '0' },
                                        }
                                     );
                    print_Inventory_Holiday.txtStartNumber.Text = (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0)
                     ? dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim()
                     : dtBoxNumberStart.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();

                    print_Inventory_Holiday.txtDateCode.Text = dayYearPart;
                    print_Inventory_Holiday.txtLotNoInventory.Text = check_LotNo.txtCheckLotNo.Text;

                    bool flagUpdate = false;
                    flagUpdate = (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0) ? false : true;
                    if (print_Inventory_Holiday.ShowDialog() == true)
                    {
                        string startNumber = print_Inventory_Holiday.txtStartNumber.Text;
                        string dateCodePrintInventory = print_Inventory_Holiday.txtDateCode.Text;
                        string txtLotNo = print_Inventory_Holiday.txtLotNoInventory.Text;
                        string txtNumberLabel = print_Inventory_Holiday.txtNumberLabel.Text;
                        DBAgent dbNumberLabel = DBAgent.Instance;
                        DataTable dtNumberLabel = dbNumberLabel.GetData(
                            "SELECT SUM(NUMBER_LABEL) AS TotalNumberLabel FROM M_BAR_HISTORY WHERE STATUS = @INVENTORY_PRINT and LOT_NO = @LOT_NO",
                            new Dictionary<string, object> {
                        { "@INVENTORY_PRINT", "INVENTORY_PRINT" },
                        { "@LOT_NO",  txtLotNo},
                            }
                        );

                        if (dtNumberLabel != null && dtNumberLabel.Rows.Count > 0)
                        {
                            string numberSerial = dtNumberLabel.Rows[0]["TotalNumberLabel"].ToString().Trim();
                            if (numberSerial != "")
                            {
                                int tong = int.Parse(numberSerial) + int.Parse(txtNumberLabel);
                                if (tong > 1000)
                                {
                                    System.Windows.Forms.MessageBox.Show($"Số lượng nhãn còn lại có thể in tồn là: {1000 - int.Parse(numberSerial)}! Vui lòng nhập lại", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }
                        }
                        string txtSerialStartUpdate = "";
                        string txtSerialUpdate = "";
                        string txtSerialInsert = "";
                        if (print_Inventory_Holiday.DialogResult == true)
                        {
                            try
                            {
                                int numberLabel = Int16.Parse(print_Inventory_Holiday.txtNumberLabel.Text);

                                DateTime currentTime = DateTime.Now;
                                if (Properties.Settings.Default.FlagCheck == true)
                                {
                                    Properties.Settings.Default.FlagCheck = false;
                                    Properties.Settings.Default.Count = 0;
                                    Properties.Settings.Default.Save();
                                }
                                Properties.Settings.Default.Count += numberLabel;
                                Properties.Settings.Default.Save();
                                if (numberLabel >= 1001)
                                {
                                    System.Windows.Forms.MessageBox.Show("Nhập vượt quá số lượng cần in tồn!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                                if (this._basePath != null)
                                {
                                    try
                                    {
                                        this._bartenderEngine.OpenFormat(this._basePath);
                                        string txtLot = "";
                                        string txtProduct = "";
                                        string txtDatecode = "";
                                        string txtSerial = "";
                                        if (this.ComboBoxPrintersList.Text == "" || this.ComboBoxPrintersList.Text == null)
                                        {
                                            System.Windows.Forms.MessageBox.Show("Vui lòng chọn máy in!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }

                                        var txtBoxLot = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Lot");

                                        if (CheckProductionInventory(Path.GetFileName(this._basePath), txtLotNo))
                                        {
                                            foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                            {
                                                foreach (TemplateField field in barcodeField.Fields)
                                                {
                                                    var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                    if (field.Name.Contains("txt_Lot"))
                                                    {
                                                        txtLot = txtLotNo;
                                                        field.BtwValue = txtLot;
                                                        this._bartenderEngine.SetValue(field.Name, txtLot);
                                                    }
                                                }
                                                foreach (TemplateField field in barcodeField.UncheckFields)
                                                {
                                                    var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                    if (field.Name.Contains("txt_Serial"))
                                                    {
                                                        txtSerial = startNumber.ToString();
                                                        field.BtwValue = "13";
                                                        this._bartenderEngine.SetValue(field.Name, txtSerial);
                                                    }
                                                    if (field.Name.Contains("txt_Product"))
                                                    {
                                                        txtProduct = txtBox.Text;
                                                        field.BtwValue = txtProduct;
                                                        this._bartenderEngine.SetValue(field.Name, txtProduct);
                                                    }
                                                    if (field.Name.Contains("txt_Datecode"))
                                                    {
                                                        txtDatecode = dateCodePrintInventory;
                                                        field.BtwValue = dateCodePrintInventory;
                                                        this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                    }
                                                }
                                            }

                                            if (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0)
                                            {
                                                DBAgent dbserialSample = DBAgent.Instance;
                                                DataTable dtserialSample = dbserialSample.GetData(
                                                    "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                                    new Dictionary<string, object> {
                                                 { "@file_name", Path.GetFileName(this._basePath) }
                                                    }
                                                 );
                                                string txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                                if (txtSerialStartInsert.Count() == 5)
                                                {
                                                    txtSerialStartUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                                    txtSerialUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(txtNumberLabel));
                                                }
                                                if (txtSerialStartInsert.Count() == 4)
                                                {
                                                    txtSerialStartUpdate = V80E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                                    txtSerialUpdate = V80E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(txtNumberLabel));
                                                }
                                                if (txtSerialStartInsert.Count() == 3)
                                                {
                                                    txtSerialStartUpdate = V12E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                                    txtSerialUpdate = V12E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(txtNumberLabel));
                                                }

                                                foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                                {
                                                    foreach (TemplateField field in barcodeField.Fields)
                                                    {
                                                        var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                        if (field.Name.Contains("txt_Lot"))
                                                        {
                                                            txtLot = txtLotNo;
                                                            field.BtwValue = txtLot;
                                                            this._bartenderEngine.SetValue(field.Name, txtLot);
                                                        }

                                                        if (field.Name.Contains("txt_Datecode"))
                                                        {
                                                            txtDatecode = txtBox.Text;
                                                            field.BtwValue = txtDatecode;
                                                            this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                        }
                                                    }
                                                    foreach (TemplateField field in barcodeField.UncheckFields)
                                                    {
                                                        var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                        if (field.Name.Contains("txt_Product"))
                                                        {
                                                            txtProduct = txtBox.Text;
                                                            field.BtwValue = txtProduct;
                                                            this._bartenderEngine.SetValue(field.Name, txtProduct);
                                                        }
                                                        if (field.Name.Contains("txt_Serial"))
                                                        {
                                                            field.BtwValue = txtSerialStartUpdate;
                                                            this._bartenderEngine.SetValue(field.Name, txtSerialStartUpdate);
                                                        }
                                                        if (field.Name.Contains("txt_Datecode"))
                                                        {
                                                            txtDatecode = dayYearPart;
                                                            field.BtwValue = txtDatecode;
                                                            this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                        }
                                                    }
                                                }
                                                flagUpdate = true;
                                            }
                                            else
                                            {
                                                DBAgent dbserialSample = DBAgent.Instance;
                                                DataTable dtserialSample = dbserialSample.GetData(
                                                    "Select TOP(1) * from M_BARTENDER where FILENAME = @file_name",
                                                    new Dictionary<string, object> {
                                        { "@file_name", Path.GetFileName(this._basePath) }
                                                    }
                                                 );
                                                if (dtserialSample != null && dtserialSample.Rows.Count > 0)
                                                {
                                                    foreach (BarcodeField barcodeField in this._btwData.Barcodes)
                                                    {
                                                        foreach (TemplateField field in barcodeField.Fields)
                                                        {
                                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                            if (field.Name.Contains("txt_Lot"))
                                                            {
                                                                txtLot = txtLotNo;
                                                                field.BtwValue = txtLot;
                                                                this._bartenderEngine.SetValue(field.Name, txtLot);
                                                            }

                                                            if (field.Name.Contains("txt_Datecode"))
                                                            {
                                                                txtDatecode = txtBox.Text;
                                                                field.BtwValue = txtDatecode;
                                                                this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                            }
                                                        }
                                                        foreach (TemplateField field in barcodeField.UncheckFields)
                                                        {
                                                            var txtBox = (System.Windows.Controls.TextBox)this.FindName("_name_" + field.Name);
                                                            if (field.Name.Contains("txt_Product"))
                                                            {
                                                                txtProduct = txtBox.Text;
                                                                field.BtwValue = txtProduct;
                                                                this._bartenderEngine.SetValue(field.Name, txtProduct);
                                                            }
                                                            if (field.Name.Contains("txt_Datecode"))
                                                            {
                                                                txtDatecode = dayYearPart;
                                                                field.BtwValue = txtDatecode;
                                                                this._bartenderEngine.SetValue(field.Name, txtDatecode);
                                                            }
                                                            if (field.Name.Contains("txt_Serial"))
                                                            {
                                                                string txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                                                if (txtSerialStartInsert.Count() == 5)
                                                                    txtSerialInsert = CV60E(txtSerialStartInsert, int.Parse(txtNumberLabel) - 1);

                                                                else if (txtSerialStartInsert.Count() == 3)
                                                                {
                                                                    txtSerialInsert = V12E(txtSerialStartInsert, int.Parse(txtNumberLabel) - 1);
                                                                }
                                                                else
                                                                {
                                                                    txtSerialInsert = V80E(txtSerialStartInsert, int.Parse(txtNumberLabel) - 1);

                                                                }
                                                                field.BtwValue = txtSerialStartInsert;
                                                                this._bartenderEngine.SetValue(field.Name, txtSerialStartInsert);
                                                            }
                                                        }
                                                    }
                                                    flagUpdate = false;
                                                }
                                                else
                                                {
                                                    System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Lot.\nXem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                }
                                            }
                                            if (flagUpdate == true)
                                            {
                                                try
                                                {
                                                    DBAgent db = DBAgent.Instance;
                                                    string query = "UPDATE M_BARTENDER_PRINT SET SERIAL_NUMBER = @SERIAL_NUMBER, UPDATE_DATE = @update_date, UPDATE_BY = @update_by WHERE FILE_NAME = @filename AND DATE_CODE = @lot_no AND FLAG_REPRINT = @flag_print";
                                                    Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@SERIAL_NUMBER", txtSerialUpdate },
                                            { "@update_date", DateTime.Now },
                                            { "@update_by", Department },
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@lot_no", dayYearPart },
                                            { "@flag_print", '0' },
                                        };

                                                    db.Execute(query, parameters);
                                                }
                                                catch
                                                {
                                                    System.Windows.Forms.MessageBox.Show("Không thể cập nhật dữ liệu!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    int box_number = int.Parse(CopiesOfLabel.Text);
                                                    DBAgent db = DBAgent.Instance;
                                                    string query = "INSERT INTO M_BARTENDER_PRINT (FILE_NAME, DATE_CODE, SERIAL_NUMBER, FLAG_REPRINT,NUMBER_REPRINT, CREATE_DATE,UPDATE_DATE, CREATE_BY, UPDATE_BY) VALUES (@filename, @date, @box_number, @flag_print,@number_reprint, @create_date,@update_date, @create_by,@update_by)";
                                                    Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", dayYearPart },
                                            { "@box_number", txtSerialInsert },
                                            { "@flag_print", '0' },
                                            { "@number_reprint", DBNull.Value },
                                            { "@create_date", DateTime.Now },
                                            { "@update_date",DBNull.Value },
                                            { "@create_by", Department },
                                            { "@update_by", DBNull.Value }

                                        };
                                                    db.Execute(query, parameters);
                                                }
                                                catch
                                                {
                                                    System.Windows.Forms.MessageBox.Show("Không thể thêm dữ liệu mới!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                }
                                            }
                                            DBAgent db_ = DBAgent.Instance;
                                            int numberlabel = int.Parse(CopiesOfLabel.Text);
                                            string resultNotNull = null;

                                            if (!string.IsNullOrEmpty(txtSerialUpdate))
                                            {
                                                resultNotNull = txtSerialUpdate;
                                            }
                                            else if (!string.IsNullOrEmpty(txtSerialInsert))
                                            {
                                                resultNotNull = txtSerialInsert;
                                            }


                                            string query_bartender_history = "INSERT INTO M_BAR_HISTORY (FILE_NAME, DATE_CODE, LOT_NO, GOOGLE_NUMBER,STR_SERIAL,END_SERIAL,CREATE_DATE, CREATE_BY,NUMBER_LABEL,STATUS) VALUES (@filename, @date, @lot_no, @google_number,@str_serial, @end_serial,@create_date, @create_by,@number_label,@status)";
                                            Dictionary<string, object> parameters_bartender_history = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@date", dayYearPart },
                                            { "@lot_no", txtLot },
                                            { "@google_number", GoogleNumber == null?"": GoogleNumber},
                                            { "@str_serial", startNumber },
                                            { "@end_serial", resultNotNull},
                                            { "@create_date", DateTime.Now },
                                            { "@create_by", Department },
                                            { "@number_label", numberLabel },
                                            { "@status", "INVENTORY_HOLIDAY" },
                                        };

                                            db_.Execute(query_bartender_history, parameters_bartender_history);
                                            PrintJob printJob = new PrintJob();
                                            printJob.Serializiers = numberLabel;
                                            printJob.CopiesOfLabel = 1;
                                            printJob.Path = this._basePath;
                                            printJob.Printer = this.ComboBoxPrintersList.Text;
                                            Seagull.BarTender.Print.Result result = this._bartenderEngine.Print(printJob);
                                            if (result == Seagull.BarTender.Print.Result.Success)
                                            {
                                                System.Windows.Forms.MessageBox.Show("In thành công!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }
                                            else
                                            {
                                                System.Windows.Forms.MessageBox.Show("In thất bại!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }
                                        }

                                        else
                                        {
                                            System.Windows.Forms.MessageBox.Show("Không có dữ liệu về Product. Xem lại kế hoạch sản xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Windows.Forms.MessageBox.Show("Có lỗi xảy ra!" + ex, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                                else
                                {
                                    System.Windows.Forms.MessageBox.Show("Vui lòng chọn file!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch
                            {
                                System.Windows.MessageBox.Show("Vui lòng nhập kiểu số!");
                            }
                        }
                    }
                }
            }
        }
    }
    class FileBox
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public FileBox(string fileName, string filePath)
        {
            this.FileName = fileName;
            this.FilePath = filePath;
        }
    }
}
