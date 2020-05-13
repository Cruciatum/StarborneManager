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
        public string password { get; set; }
        public int port { get; set; }
        public string host { get; set; }
        public string db { get; set; }
        public string username { get; set; }
        public string instance { get; set; }

        public DBSettings() { }
        public DBSettings(string jsonFileLoc)
        {
            using (StreamReader r = new StreamReader(jsonFileLoc))
            {
                string json = r.ReadToEnd();
                DBSettings d = JsonConvert.DeserializeObject<DBSettings>(json);
                this.password = d.password;
                this.port = d.port;
                this.host = d.host;
                this.db = d.db;
                this.username = d.username;
                this.instance = d.instance;
            }
        }
    }
}
