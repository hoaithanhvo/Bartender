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

namespace BarcodeCompareSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private EngineWrapper _bartenderEngine;
        private IList<string> _printers;
        private string _basePath;
        private string thumbnailFile = "";
        private FolderBrowserDialog _folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
        private Hashtable listItems;
        private PrintJob _printJob = new PrintJob();
        private BtwData _btwData;
        private PrintJobQueue printJobQueue;
        private string[] browsingFormats; // The list of filenames in the current folder
        private string BARCODE_NAME = "txt_Serial";
        private string SERVER_NOT_VALID = "Serial is not valid!";
        private string ZERO = "0";
        private string configPath;
        //thanh add path get file
        private string pathGetFile;
        const string CONFIG_FILE = "config.ini";
        string Department;
        string serial_sequence; 

        public IList<string> Printers
        {
            get { return _printers; }
            set
            {
                _printers = value;
                OnPropertyChanged("Printers");
            }
        }
        //thanh cmt main
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            this.Printers = this.GetInstalledPrinters();
            this.DataContext = this;
            this.unCheckFieldStackPanel.IsEnabled = false;
            this._bartenderEngine = new EngineWrapper();
            //thanh add
            //this.Button_RePrint.IsEnabled = false;
            //this.Button_Print.IsEnabled = false;

            // Initialize a list and a queue.
            listItems = new System.Collections.Hashtable();
            printJobQueue = new PrintJobQueue(_bartenderEngine);

            //thanh add load file from path config.init
            LoadFilePath();
        }
        //thanh add load tat ca cac file .btw tu config.ini
        private void LoadFilePath()
        {

            try
            {
                var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(directory + "\\" + CONFIG_FILE);
                pathGetFile = data["Database"]["Path"];
                Department = data["Database"]["Department"];

                this.listItems.Clear();
                this.txtModel.Text = "";
                this.browsingFormats = System.IO.Directory.GetFiles(pathGetFile, "*.btw");
                this.lstLabelBrowser.ItemsSource = this.ParserFileList(browsingFormats);

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Không tìm thấy file trong config" , "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //throw new DBException("");
            }
        }

        //THANH ADD 25/12/2023 tìm kiếm theo Model
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
            catch(Exception error)
            {
                System.Windows.Forms.MessageBox.Show("Có lỗi xảy ra. Liên hệ System!" + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //App.DebugLog(error.StackTrace);
            }
        }

        //thanh cmt sẽ đưa danh sách có được vào List File để add vào ListView
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


        //Thanh add cmt click item listView
        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        //Thanh98 add cmt load dữ liệu
        private void LoadBtwData()
        {
            try
            {
                serial_sequence = "";

                this._bartenderEngine.OpenFormat(this._basePath);
                this._btwData = new BtwData();
                this._btwData.FileName = Path.GetFileName(this._basePath);
                this._btwData.Path = this._basePath;

                //thanh98 add load data sql
                List<Barcode> barcodesIni = ConfigFile.GetFields(this._btwData.FileName);

                BarcodeField barcodeFields;
                foreach (Barcode barcode in barcodesIni)
                {
                    
                    barcodeFields = new BarcodeField();

                    //thanh cmt đọc giá trị các trường Fields
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
                        if(fieldName != "txt_Serial")
                        {
                            TemplateField field = new TemplateField();
                            string value = this._bartenderEngine.ReadValue(fieldName);
                            field.Name = fieldName;
                            field.BtwValue = value;
                            barcodeFields.UncheckFields.Add(field);
                        }    
                        else
                        {
                            //lay serial hien tai + 1
                            serial_sequence = barcode.serial_sequence;
                            //string nextNumber = GetNextSerial(barcode.numberSerial, 1);

                            TemplateField field = new TemplateField();
                            field.Name = fieldName;
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
        //UI image và allow edit, auto increase
        private void RenderUIUsingBtwData()
        {
            this.Dispatcher.Invoke(() =>
            {
                try {
                    thumbnailFile = this._bartenderEngine.ExportImage(700, 1000);
                    picThumbnail.Source = new BitmapImage(new Uri(thumbnailFile));
                } catch (Exception e) {
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
                            label.Margin = new System.Windows.Thickness(20,20,20,5); 
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
                            if (field.Name == "txt_Serial")
                            {
                                tbxValue.Visibility = System.Windows.Visibility.Hidden;
                                label.Visibility = System.Windows.Visibility.Hidden;
                            }
                               
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

        //thanh cmt button in lai
        private void Button_Re_Print_Click(object sender, RoutedEventArgs e)
        {

            LoginForm login = new LoginForm();
            if (login.ShowDialog() == true) {
                if (login.DialogResult == true) {

                    switch(Authenticate.IsUserExist(login.txtUsername.Text, login.txtPassword.Password))
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
            if (this._bartenderEngine != null) {
                this._bartenderEngine.Dispose();
            }
        }

        //thanh98 add tang tu dong so Serial
        private string GetNextSerial(string currentNumber, int labelCount)
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
                        
                        if(A == currentNumber)
                        {
                            checkCompare = true;
                            Console.WriteLine($"{hangTram[i]}{hangChuc[j]}{hangDonVi[k]}");
                        }
                        if(checkCompare == true)
                        {
                            if(count == labelCount)
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

        //thanh add CV60
        //thanh98 model CV60E
        private string CV60E(string currentNumber, int labelCount)
        {
            char[] hangChucNghin = serial_sequence.Trim().ToCharArray();
            char[] hangNghin = serial_sequence.Trim().ToCharArray();
            char[] hangTram = serial_sequence.Trim().ToCharArray();
            char[] hangChuc = serial_sequence.Trim().ToCharArray();
            char[] hangDonVi = serial_sequence.Substring(1).Trim().ToCharArray();

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

        //thanh add cmt chuc nang IN (false), In lại (true)
        private void Print()
        {

            if (this._basePath != null)
            {
                try
                {
                    //thanh kiem tra thong tin so luong nhan co phai la interger
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

                    var txtBoxLot = (System.Windows.Controls.TextBox)this.FindName("_name_" + "txt_Lot");

                    if (CheckProduction(Path.GetFileName(this._basePath), txtBoxLot.Text))
                    {
                        //chọn may in
                        if (this.ComboBoxPrintersList.Text != "" && this.ComboBoxPrintersList.Text != null)
                        {
                            //flag du lieu da ton tai hay chua
                            bool flagUpdate = false;
                            //thanh lay so serial tu M_BarTender
                            DBAgent dbBoxNumber = DBAgent.Instance;
                            DataTable dtBoxNumber = dbBoxNumber.GetData(
                                "Select TOP(1) * from M_BARTENDER_PRINT where FILE_NAME = @file_name and LOT_NO = @lot and FLAG_REPRINT = @flag_reprint;",
                                new Dictionary<string, object> {
                                    { "@file_name", Path.GetFileName(this._basePath) },
                                    { "@lot", txtBoxLot.Text },
                                    { "@flag_reprint", '0' }
                                }
                             );

                            if (dtBoxNumber != null && dtBoxNumber.Rows.Count > 0)
                            {
                                //xu lý txtSerial tang gia tri 1 don vi
                                //txtSerialStartUpdate = GetNextSerial(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                //txtSerialUpdate = GetNextSerial(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(CopiesOfLabel.Text));
                                txtSerialStartUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), 1);
                                txtSerialUpdate = CV60E(dtBoxNumber.Rows[0]["SERIAL_NUMBER"].ToString().Trim(), int.Parse(CopiesOfLabel.Text));

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
                                        //thanh add txtSerial
                                        if (field.Name.Contains("txt_Serial"))
                                        {
                                            field.BtwValue = txtSerialStartUpdate;
                                            this._bartenderEngine.SetValue(field.Name, txtSerialStartUpdate);
                                        }
                                    }
                                }
                                flagUpdate = true;
                            }
                            else
                            {
                                //thanh lay so serial tu M_BarTender
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
                                            //thanh add txtSerial
                                            if (field.Name.Contains("txt_Serial"))
                                            {
                                                
                                                txtSerialStartInsert = dtserialSample.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();
                                                if (int.Parse(CopiesOfLabel.Text) == 1)
                                                    txtSerialInsert = txtBox.Text;
                                                else
                                                    txtSerialInsert = CV60E(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);
                                                //txtSerialInsert = GetNextSerial(txtSerialStartInsert, int.Parse(CopiesOfLabel.Text) - 1);

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

                            //bat dau cap nhat du lieu true cap nhat/ false thi insert
                            if (flagUpdate == true)
                            {
                                try
                                {
                                    //cap nhat du lieu
                                    //int box_number = int.Parse(CopiesOfLabel.Text) - 1;
                                    DBAgent db = DBAgent.Instance;
                                    string query = "UPDATE M_BARTENDER_PRINT SET SERIAL_NUMBER = @box, UPDATE_DATE = @update_date, UPDATE_BY = @update_by WHERE FILE_NAME = @filename AND LOT_NO = @lot_no AND FLAG_REPRINT = @flag_print";

                                    Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@box", txtSerialUpdate },
                                            { "@update_date", DateTime.Now },
                                            { "@update_by", Department },
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@lot_no", txtLot },
                                            { "@flag_print", '0' }
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
                                    string query = "INSERT INTO M_BARTENDER_PRINT (FILE_NAME, LOT_NO, SERIAL_NUMBER, FLAG_REPRINT,NUMBER_REPRINT, CREATE_DATE,UPDATE_DATE, CREATE_BY, UPDATE_BY) VALUES (@filename, @lotno, @box_number, @flag_print,@number_reprint, @create_date,@update_date, @create_by,@update_by)";

                                    Dictionary<string, object> parameters = new Dictionary<string, object> {
                                            { "@filename", Path.GetFileName(this._basePath) },
                                            { "@lotno", txtLot },
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

                            //thanh add Lien ket voi BarTender de in
                            PrintJob printJob = new PrintJob();
                            printJob.Serializiers = int.Parse(CopiesOfLabel.Text);
                            printJob.CopiesOfLabel = 1;
                            printJob.Path = this._basePath;
                            printJob.Printer = this.ComboBoxPrintersList.Text;
                            Seagull.BarTender.Print.Result result = this._bartenderEngine.Print(printJob);

                            System.Windows.Forms.MessageBox.Show("In thanh cong!", "Comfirm", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                                //thanh add check Model và Lot có ở T_LOT_PRODUCT
                                if (CheckProduction(Path.GetFileName(this._basePath), txtBoxLot.Text))
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
                                                txtLot = txtBox.Text;
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
                                        }
                                    }

                                    //them du lieu in lai
                                    try
                                    {
                                        //them du lieu
                                        int box_number = int.Parse(CopiesOfLabel.Text);
                                        DBAgent db = DBAgent.Instance;
                                        string query = "INSERT INTO M_BARTENDER_PRINT (FILE_NAME, LOT_NO, SERIAL_NUMBER, FLAG_REPRINT,NUMBER_REPRINT, CREATE_DATE,UPDATE_DATE, CREATE_BY, UPDATE_BY) VALUES (@filename, @lotno, @box_number, @flag_print,@number_reprint, @create_date,@update_date, @create_by,@update_by)";

                                        Dictionary<string, object> parameters = new Dictionary<string, object> {
                                                    { "@filename", Path.GetFileName(this._basePath) },
                                                    { "@lotno", txtLot },
                                                    { "@box_number", startNumber },
                                                    { "@flag_print", '1' },
                                                    { "@number_reprint", numberLabel },
                                                    { "@create_date", DateTime.Now },
                                                    { "@update_date", DBNull.Value },
                                                    { "@create_by", username },
                                                    { "@update_by", DBNull.Value }
                                                };
                                        db.Execute(query, parameters);
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
        
        //thanh add kt co ton tai trong ke hoach san xuat
        private bool CheckProduction(string filename, string lotno)
        {
            try
            {
                DBAgent dbProduct = DBAgent.Instance;
                DataTable dtProduct = new DataTable();
                string query = @"
                    SELECT A.PRODUCT_NO
                    FROM T_LOT_PRODUCT AS A
                    INNER JOIN M_BARTENDER AS B ON A.PRODUCT_NO = B.PRODUCT_NO
                    WHERE B.FILENAME = @filename AND A.LOT_NO = @lotno";

                Dictionary<string, object> parameters = new Dictionary<string, object> {
                    { "@filename", filename },
                    { "@lotno", lotno },
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
            catch (Exception ex) {
            }
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.Refresh_Form();
        }

        private void Refresh_Form() {
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

        //thanh add Reload sau khi cap nhat du lieu

        private void Button_Reload(object sender, RoutedEventArgs e)
        {
            LoadFilePath();
            //Refresh_Form();
            this.Refresh_Button.IsEnabled = false;
            this._bartenderEngine.CloseFormat(false);
            this.ClearStackPanel();
        }

        //thanh add nut in thu
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


                }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show("Lỗi in thử\n Vui lòng thử lại!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
