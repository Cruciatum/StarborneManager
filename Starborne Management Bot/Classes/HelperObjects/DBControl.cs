using IBM.Data.DB2.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starborne_Management_Bot.Classes.HelperObjects
{
    internal static class DBControl
    {
        internal static DBSettings dbSettings;
        static DB2ConnectionStringBuilder sBuilder = new DB2ConnectionStringBuilder();
        static DB2Connection conn = new DB2Connection();
        
        internal static void UpdateDB(string sql)
        {
            sBuilder.Database = dbSettings.db;
            sBuilder.UserID = dbSettings.username;
            sBuilder.Password = dbSettings.password;
            sBuilder.Server = dbSettings.host + ":" + dbSettings.port;
            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                DB2Command cmd = new DB2Command(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close(); conn.Dispose();
            }
        }
    }
}
