using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BarcodeCompareSystem
{
    public class Authenticate
    {
        protected const string m_EncryptKey = "@Nidec";
        //thanh add ma hoa mat khau
        public static string Encrypt(string toEncrypt, bool useHashing)
        {

            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            //If hashing use get hashcode regards to your key
            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(m_EncryptKey));
                //Always release the resources and flush data
                // of the Cryptographic service provide. Best Practice
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(m_EncryptKey);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
              cTransform.TransformFinalBlock(toEncryptArray, 0,
              toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();
            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static int IsUserExist(string username, string password)
        {
            //string hashedPassword = CalculateMD5Hash(password); // Mã hóa mật khẩu nhập vào thành MD5
            string hashedPassword = Encrypt(password + username.ToLower(), true);
            DBAgent db = DBAgent.Instance;
            object id = db.GetValue("SELECT id FROM M_USER WHERE UserName = @username;",
                                    new Dictionary<string, object> { { "@username", username } });

            if (id != null)
            {
                object storedPassword = db.GetValue("SELECT PassWord FROM M_USER WHERE UserName = @username;",
                                                    new Dictionary<string, object> { { "@username", username } });

                if (storedPassword != null)
                {
                    string dbPassword = storedPassword.ToString();
                    if (dbPassword.Equals(hashedPassword))
                    {
                        // Người dùng tồn tại và mật khẩu đúng
                        return 1;
                    }
                    else
                    {
                        // Người dùng tồn tại nhưng mật khẩu không đúng
                        return 2;
                    }
                }
            }

            // Người dùng không tồn tại trong cơ sở dữ liệu
            return 0;
        }

    }
}
