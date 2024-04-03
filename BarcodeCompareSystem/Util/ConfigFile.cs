using System;
using System.Collections.Generic;
using System.Windows;
using BarcodeCompareSystem.Model;
using System.Data;
using System.Windows.Forms;
using log4net.Util;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Globalization;
using System.Linq;

namespace BarcodeCompareSystem.Util
{
    class ConfigFile
    {
        public static int start1;
        public static List<Barcode> GetFields(string fileName,string [] ModelYear2NumberArray,string dayYearPart)
        {
            List<Barcode> sections = new List<Barcode>();
            try
            {
                string fileNameCheck = fileName.Substring(0, fileName.Length - 4); ;
                //sql
                DBAgent db = DBAgent.Instance;
                DataTable dt = new DataTable();
                DataTable dt1 = new DataTable();
                string query = @"
                    SELECT *
                    FROM M_BARTENDER AS A
                    LEFT JOIN M_BARTENDER_PRINT AS B ON A.FILENAME = B.FILE_NAME and B.DATE_CODE = @result
                    WHERE A.FILENAME = @nme";

                Dictionary<string, object> parameters = new Dictionary<string, object> {
                    { "@nme", fileName },
                    { "@result", dayYearPart },
                };
                dt = db.GetData(query, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string fields = dt.Rows[0]["ALLOW_EDIT"].ToString().Trim();
                    string uncheckfields = dt.Rows[0]["AUTO_INCREASE"].ToString().Trim();
                    string numberSerial = dt.Rows[0]["SERIAL_NUMBER"].ToString().Trim();
                    string serial_sequence = dt.Rows[0]["SERIAL_SEQUENCE"].ToString().Trim();
                    string product_id = dt.Rows[0]["PRODUCT_NO"].ToString().Trim();
                    string query1 = @"
                        SELECT M_SHIFT.START1
                        FROM T_LOT_PRODUCT 
                        JOIN M_SHIFT ON T_LOT_PRODUCT.DIVISION_CD = M_SHIFT.DIVISION_CD AND T_LOT_PRODUCT.PROCESS_CD = M_SHIFT.PROCESS_CD
                        WHERE T_LOT_PRODUCT.PRODUCT_NO = @product_id AND M_SHIFT.SHIFT_CD = @SHIFT_CD";
                                        Dictionary<string, object> parameters1 = new Dictionary<string, object> {
                        { "@product_id", product_id },
                         { "@SHIFT_CD", "CA1" }

                    };

                    DataTable result1 = db.GetData(query1, parameters1);

                    if (result1 != null && result1.Rows.Count > 0)
                    {
                        string DATE = result1.Rows[0]["START1"].ToString().Trim();
                        DateTime start = DateTime.ParseExact(DATE, "HH:mm:ss", CultureInfo.InvariantCulture);
                        start1 = start.Hour;
                    }
                    if (numberSerial != "")
                    {
                        numberSerial = dt.Rows[0]["SERIAL_NUMBER"].ToString().Trim();
                    }
                    else
                    {
                        numberSerial = dt.Rows[0]["SERIAL_SAMPLE"].ToString().Trim();

                    }

                    Barcode barcode = new Barcode();
                    //cat theo dau ',' doi voi fields
                    barcode.Fields = fields.Split(',');
                    Array.Sort(barcode.Fields);
                    //cat theo dau ',' doi voi uncheckfields
                    barcode.UnCheckFields = uncheckfields.Split(',');
                    Array.Sort(barcode.UnCheckFields);
                    //add serial 
                    barcode.numberSerial = numberSerial;
                    barcode.serial_sequence = serial_sequence;
                    sections.Add(barcode);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Không có dữ liệu về File", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Lỗi truy vấn dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return sections;
        }
    }
}
