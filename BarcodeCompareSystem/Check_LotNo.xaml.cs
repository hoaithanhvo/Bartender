using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BarcodeCompareSystem
{
    /// <summary>
    /// Interaction logic for Check_LotNo.xaml
    /// </summary>
    public partial class Check_LotNo : Window
    {
        public Check_LotNo()
        {
            InitializeComponent();
            CenterWindowOnScreen();
        }
        public void CenterWindowOnScreen()
        {
            // Lấy độ rộng và chiều cao của màn hình
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Đặt vị trí của cửa sổ ở chính giữa màn hình
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = (screenHeight - this.Height) / 2;
        }

        private void btnCheckLotNo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void btnCancelCheckLotNo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
