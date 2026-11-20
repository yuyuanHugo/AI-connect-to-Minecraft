using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace McBot
{
    public class AiConfig
    {
        public string provider { get; set; }
        public string baseURL { get; set; }
        public string apiKey { get; set; }
        public string model { get; set; }
        public double temperature { get; set; }
        public string systemPrompt { get; set; }

        public AiConfig()
        {
            provider = "openai";
            baseURL = "https://api.openai.com/v1";
            apiKey = "";
            model = "gpt-4o-mini";
            temperature = 0.7;
            systemPrompt = "你是一个 Minecraft 游戏内的 AI 机器人。你可以移动、跟随玩家、说话。回复要简短自然，像真人玩家。";
        }

        public string Label
        {
            get
            {
                if (provider == "openai") return "OpenAI";
                if (provider == "deepseek") return "DeepSeek";
                if (provider == "moonshot") return "Moonshot";
                return "自定义";
            }
        }
    }

    public class CharConfig
    {
        public string username { get; set; }
        public string skinPath { get; set; }
        public string skinModel { get; set; }
        public string nickname { get; set; }   // AI 的名字（鲸鲸 / 龙娘 …）

        public CharConfig()
        {
            username = "MineBot";
            skinPath = "";
            skinModel = "steve";
            nickname = "鲸鲸";
        }
    }

    public class ClientConfig
    {
        public string version { get; set; }
        public string route { get; set; }

        public ClientConfig()
        {
            version = "1.21.4";
            route = "official";
        }
    }

    public class BotConfig
    {
        public bool selfAware { get; set; }
        public int selfAwareInterval { get; set; }
        public string engine { get; set; }      // 固定 ovo
        public string replyMode { get; set; }   // whisper | public
        public bool whitelistEnabled { get; set; }   // 只回应白名单玩家
        public string whitelist { get; set; }        // 逗号分隔的玩家名

        public BotConfig()
        {
            selfAware = true;
            selfAwareInterval = 30;
            engine = "ovo";
            replyMode = "whisper";
            whitelistEnabled = false;
            whitelist = "";
        }
    }

    public class ServerEntry
    {
        public string id { get; set; }
        public string host { get; set; }
        public int port { get; set; }
        public string version { get; set; }
        public string note { get; set; }

        public ServerEntry()
        {
            id = "";
            host = "";
            port = 25565;
            version = "1.21.4";
            note = "";
        }

        public ServerEntry Clone()
        {
            var s = new ServerEntry();
            s.id = id; s.host = host; s.port = port; s.version = version; s.note = note;
            return s;
        }
    }

    public class Config
    {
        public AiConfig ai { get; set; }
        public CharConfig character { get; set; }
        public ClientConfig client { get; set; }
        public BotConfig bot { get; set; }
        public List<ServerEntry> servers { get; set; }
        public string selectedServerId { get; set; }

        public Config()
        {
            ai = new AiConfig();
            character = new CharConfig();
            client = new ClientConfig();
            bot = new BotConfig();
            servers = new List<ServerEntry>();
            selectedServerId = null;
        }

        public ServerEntry SelectedServer
        {
            get
            {
                if (selectedServerId == null) return null;
                for (int i = 0; i < servers.Count; i++)
                    if (servers[i].id == selectedServerId) return servers[i];
                return null;
            }
        }
    }

    public static class Store
    {
        public static string AppDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string ConfigPath
        {
            get
            {
                string custom = Environment.GetEnvironmentVariable("MCBOT_CONFIG");
                if (!string.IsNullOrEmpty(custom)) return custom;
                return Path.Combine(Runtime.Dir, "config.json");
            }
        }

        public static string DataPath(string name)
        {
            return Path.Combine(Path.Combine(AppDir, "data"), name);
        }

        public static Config Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new Config();
                string json = File.ReadAllText(ConfigPath);
                var ser = new JavaScriptSerializer();
                var cfg = ser.Deserialize<Config>(json);
                if (cfg == null) return new Config();
                if (cfg.ai == null) cfg.ai = new AiConfig();
                if (cfg.character == null) cfg.character = new CharConfig();
                if (cfg.client == null) cfg.client = new ClientConfig();
                if (cfg.bot == null) cfg.bot = new BotConfig();
                if (cfg.servers == null) cfg.servers = new List<ServerEntry>();
                return cfg;
            }
            catch
            {
                return new Config();
            }
        }

        public static void Save(Config cfg)
        {
            try
            {
                var ser = new JavaScriptSerializer();
                string json = ser.Serialize(cfg);
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigPath, json, new System.Text.UTF8Encoding(false));
            }
            catch { /* 配置目录不可写时不致命 */ }
        }

        public static string NewId()
        {
            return "srv_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
