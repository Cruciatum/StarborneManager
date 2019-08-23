using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Starborne_Management_Bot
{
    internal class BotSettings
    {
        public string token { get; set; }
        public string activity { get; set; }
        public string version { get; set; }

        public BotSettings() { }
        public BotSettings(string jsonFileLoc)
        {
            using (StreamReader r = new StreamReader(jsonFileLoc))
            {
                string json = r.ReadToEnd();
                BotSettings b = JsonConvert.DeserializeObject<BotSettings>(json);
                this.token = b.token;
                this.activity = b.activity;
                this.version = b.version;
            }
        }
    }

    internal class DBSettings
    {
        public string hostname { get; set; }
        public string password { get; set; }
        public string https_url { get; set; }
        public int port { get; set; }
        public string ssldsn { get; set; }
        public string host { get; set; }
        public string jdbcurl { get; set; }
        public string uri { get; set; }
        public string db { get; set; }
        public string dsn { get; set; }
        public string username { get; set; }
        public string ssljdbcurl { get; set; }

        public DBSettings() { }
        public DBSettings(string jsonFileLoc)
        {
            using (StreamReader r = new StreamReader(jsonFileLoc))
            {
                string json = r.ReadToEnd();
                DBSettings d = JsonConvert.DeserializeObject<DBSettings>(json);
                this.hostname = d.hostname;
                this.password = d.password;
                this.https_url = d.https_url;
                this.port = d.port;
                this.ssldsn = d.ssldsn;
                this.host = d.host;
                this.jdbcurl = d.jdbcurl;
                this.uri = d.uri;
                this.db = d.db;
                this.dsn = d.dsn;
                this.username = d.username;
                this.ssljdbcurl = d.ssljdbcurl;
            }
        }
    }
}
