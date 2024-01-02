namespace BarcodeCompareSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Data.SqlClient;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Windows.Forms;
    using IniParser;
    using IniParser.Model;

    class DBAgent
    {

        const string CONFIG_FILE = "config.ini";
        const string DATABASE_SECTION = "Database";
        const string DATABASE_NAME = "Name";
        const string DATABASE_SERVER = "Server";
        const string DATABASE_TIMEOUT = "Timeout";
        const string DATABASE_USER_ID = "User";
        const string DATABASE_PASSWORD = "Password";



        private SqlConnection _connection = null;
        private static SqlTransaction trans = null;
        private static readonly DBAgent instance = new DBAgent();

        private DBAgent()
        {
            if (this._connection != null)
            {
                return;
            }
            try
            {
                var directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(directory + "\\" + CONFIG_FILE);
                this._connection = new SqlConnection("Server=" + data[DATABASE_SECTION][DATABASE_SERVER] 
                                                     + ";Database=" + data[DATABASE_SECTION][DATABASE_NAME]
                                                     + ";Connection timeout=" + data[DATABASE_SECTION][DATABASE_TIMEOUT]
                                                     + ";User Id=" + data[DATABASE_SECTION][DATABASE_USER_ID]
                                                     + ";Password=" + data[DATABASE_SECTION][DATABASE_PASSWORD]);


                this._connection.Open();
            }
            catch (Exception ex) {
                throw new DBException("");
            }
        }
        /// Create command SQLite
        private SqlCommand CreateCommand(string query, Dictionary<string, object> parameters = null)
        {
            SqlCommand cmd = new SqlCommand(query, this._connection);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, Object> entry in parameters)
                {
                    cmd.Parameters.AddWithValue(entry.Key, entry.Value);
                }
            }

            return cmd;
        }

        /// Execute query SQLite
        public int Execute(string query, Dictionary<string, object> parameters = null)
        {
            var cmd = this.CreateCommand(query, parameters);
            var ret = cmd.ExecuteNonQuery();
            cmd.Dispose();
            return ret;
        }
        /// Get value return of Execute query : get a record
        public object GetValue(string query, Dictionary<string, object> parameters = null)
        {
            var cmd = this.CreateCommand(query, parameters);
            var ret = cmd.ExecuteScalar();
            cmd.Dispose();
            return ret;
        }

        /// Get many record 
        public DataTable GetRecordSet(string query, Dictionary<string, object> parameters = null)
        {
            var ret = new DataTable();

            using (SqlCommand cmd = this.CreateCommand(query, parameters))
            {
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ret);
            }

            return ret;
        }

        /// Get a record
        public DataRow GetRecord(string query, Dictionary<string, object> parameters = null)
        {
            var dt = this.GetRecordSet(query, parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }

            return null;
        }

        //thanh add
        public DataTable GetData(string query, Dictionary<string, object> parameters = null)
        {
            var dt = this.GetRecordSet(query, parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt;
            }

            return null;
        }

        public void BeginTrans()
        {
            if (trans != null)
            {
                return;
            }

            trans = this._connection.BeginTransaction();
        }

        public void Commit()
        {
            if (trans == null)
            {
                return;
            }

            trans.Commit();
            trans.Dispose();
            trans = null;
        }

        public void Rollback()
        {
            if (trans == null)
            {
                return;
            }

            trans.Rollback();
            trans.Dispose();
            trans = null;
        }

        ///// 壓縮Sqlite檔案
        //public bool DoVacuum()
        //{
        //    try
        //    {
        //        string query = "vacuum";
        //        var cmd = this.CreateCommand(query);
        //        var ret = cmd.ExecuteNonQuery();

        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        public static DBAgent Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
