using System;
using System.Collections.Generic;
using BarcodeCompareSystem.Model;
using System.Data;
using System.Windows.Forms;

namespace BarcodeCompareSystem.Util
{
    class ConfigFile
    {

        public static List<Barcode> GetFields(string fileName) {
            List<Barcode> sections = new List<Barcode>();
            try
            {
                //sql
                DBAgent db = DBAgent.Instance;
                DataTable dt = new DataTable();
                //dt = db.GetData("Select TOP(1)* from M_BARTENDER where FILENAME=@nme;", new Dictionary<string, object> { { "@nme", fileName } });
                string query = @"
                    SELECT *
                    FROM M_BARTENDER AS A
                    LEFT JOIN M_BARTENDER_PRINT AS B ON A.FILENAME = B.FILE_NAME
                    WHERE A.FILENAME = @nme";

                Dictionary<string, object> parameters = new Dictionary<string, object> {
                    { "@nme", fileName }
                };

                dt = db.GetData(query, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string fields = dt.Rows[0]["ALLOW_EDIT"].ToString().Trim();
                    string uncheckfields = dt.Rows[0]["AUTO_INCREASE"].ToString().Trim();
                    string numberSerial = dt.Rows[0]["SERIAL_NUMBER"].ToString().Trim();
                    string serial_sequence  = dt.Rows[0]["SERIAL_SEQUENCE"].ToString().Trim();
                    //thanh lay serial de tang tu dong
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
