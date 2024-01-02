namespace BarcodeCompareSystem.Util.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    /// <summary>
    /// Database agent
    /// </summary>
    public sealed class SQLiteDBAgent
    {
        /// <summary>
        /// SQL connection
        /// </summary>
        private static SQLiteConnection conn = null;

        /// <summary>
        /// SQL transaction
        /// </summary>
        private static SQLiteTransaction trans = null;

        /// <summary>
        /// Data file path
        /// </summary>
        public string DBFilePath = SQLiteDBAgent.CombinePath(Application.StartupPath, "BackupData.db");

        /// <summary>
        /// Database agent instance
        /// </summary>
        private static readonly SQLiteDBAgent instance = new SQLiteDBAgent();

        // Explicit static constructor to tell C# compiler
        // not to mark type as before field initial
        static SQLiteDBAgent()
        {
        }

        public static string CombinePath(string directory, string file)
        {
            CultureInfo cultureInfo = new CultureInfo("en-US", false);
            return Path.Combine(string.Format(cultureInfo, directory), string.Format(cultureInfo, file));
        }


        /// <summary>
        /// Prevents a default instance of the <see cref="DatabaseAgent" /> class from being created.
        /// </summary>
        private SQLiteDBAgent()
        {
            try
            {
                // Check if db file is existing
                this.CheckDbFile();

                // conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbFilePath));
                string conn_str = string.Format("Data Source={0};Version=3;", this.DBFilePath);
                conn_str += "PRAGMA temp_store = 2;";
                conn_str += "PRAGMA page_size = 32768;";
                conn_str += "PRAGMA cache_size = 10000;";
                conn_str += "PRAGMA journal_mode = MEMORY;";

                conn = new SQLiteConnection(conn_str);
                conn.Open();
            }  
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Create command SQLite
        /// </summary>
        /// <param name="query">query to execute</param>
        /// <param name="parameters">if have parameter for query</param>
        /// <returns>SQLite Command</returns>
        private SQLiteCommand CreateCommand(string query, Dictionary<string, object> parameters = null)
        {
            SQLiteCommand cmd = new SQLiteCommand(query, conn);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, Object> entry in parameters)
                {
                    cmd.Parameters.AddWithValue(entry.Key, entry.Value);
                }
            }

            return cmd;
        }

        /// <summary>
        /// Execute query SQLite
        /// </summary>
        /// <param name="query">query string</param>
        /// <param name="parameters">parameter for query if have</param>
        /// <returns>command result</returns>
        public int Execute(string query, Dictionary<string, object> parameters = null)
        {
            var cmd = this.CreateCommand(query, parameters);
            var ret = cmd.ExecuteNonQuery();
            cmd.Dispose();
            return ret;
        }

        /// <summary>
        /// Get value return of Execute query : get a record
        /// </summary>
        /// <param name="query">string</param>
        /// <param name="parameters">parameter for query if have</param>
        /// <returns>return value</returns>
        public object GetValue(string query, Dictionary<string, object> parameters = null)
        {
            var cmd = this.CreateCommand(query, parameters);
            var ret = cmd.ExecuteScalar();
            cmd.Dispose();
            return ret;
        }

        /// <summary>
        /// Get many record 
        /// </summary>
        /// <param name="query">string</param>
        /// <param name="parameters">parameter for query if have</param>
        /// <returns></returns>
        public DataTable GetRecordSet(string query, Dictionary<string, object> parameters = null)
        {
            var ret = new DataTable();

            using (SQLiteCommand cmd = this.CreateCommand(query, parameters))
            {
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                da.Fill(ret);
            }

            return ret;
        }

        /// <summary>
        /// Get a record
        /// </summary>
        /// <param name="query">string</param>
        /// <param name="parameters">parameter for query if have</param>
        /// <returns></returns>
        public DataRow GetRecord(string query, Dictionary<string, object> parameters = null)
        {
            var dt = this.GetRecordSet(query, parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }

            return null;
        }

        /// <summary>
        /// Start transaction
        /// </summary>
        public void BeginTrans()
        {
            if (trans != null)
            {
                return;
            }

            trans = conn.BeginTransaction();
        }

        /// <summary>
        ///  Execution commit
        /// </summary>
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

        /// <summary>
        ///  Execution Rollback
        /// </summary>
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

        /// <summary>
        /// 壓縮Sqlite檔案
        /// </summary>
        /// <returns> boolean type </returns>
        public bool DoVacuum()
        {
            try
            {
                string query = "vacuum";
                var cmd = this.CreateCommand(query);
                var ret = cmd.ExecuteNonQuery();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Call instance
        /// </summary>
        public static SQLiteDBAgent Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Check file database exits
        /// </summary>
        private void CheckDbFile()
        {
            if (!File.Exists(this.DBFilePath))
            {
                throw new Exception("Database file is not exist!");
               //  throw new Exception(ErrorObjectCode.DATA_DB_FILE);
            }
        }
    }
}
