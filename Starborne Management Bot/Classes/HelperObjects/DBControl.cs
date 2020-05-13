using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starborne_Management_Bot.Classes.HelperObjects
{
    internal static class DBControl
    {
        internal static DBSettings dbSettings;
        static SqlConnectionStringBuilder sBuilder = new SqlConnectionStringBuilder();
        static SqlConnection conn = new SqlConnection();
        
        internal static void UpdateDB(string sql)
        {
            sBuilder.InitialCatalog =GlobalVars.dbSettings.db;
            sBuilder.UserID =GlobalVars.dbSettings.username;
            sBuilder.Password =GlobalVars.dbSettings.password;
            sBuilder.DataSource =GlobalVars.dbSettings.host + @"\" +GlobalVars.dbSettings.instance + "," +GlobalVars.dbSettings.port;
            conn.ConnectionString = sBuilder.ConnectionString;

            using (conn)
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close(); conn.Dispose();
            }
        }
    }
}
