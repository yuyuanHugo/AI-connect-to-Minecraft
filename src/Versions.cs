using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace McBot
{
    public class VersionInfo
    {
        public string id = "";
        public string type = "";
        public string releaseTime = "";
        public string url = "";
        public bool april = false;

        public string BadgeText
        {
            get
            {
                if (april) return "愚人节";
                if (type == "release") return "正式版";
                return "快照";
            }
        }
    }

    public static class VersionRepo
    {
        static List<VersionInfo> items = new List<VersionInfo>();
        static bool loaded = false;
        public static string LoadError = "";

        static readonly string[] AprilIds = new string[] {
            "15w14a", "1.RV-Pre1", "3D Shareware v1.34", "20w14infinite",
            "22w13oneblockatatime", "23w13a_or_b", "24w14potato", "25w14craftmine"
        };

        public static List<VersionInfo> Items
        {
            get
            {
                if (!loaded) Load();
                return items;
            }
        }

        public static void Load()
        {
            loaded = true;
            items = new List<VersionInfo>();
            string text = Runtime.ReadManifestText();
            if (text == null)
            {
                string path = Store.DataPath("version_manifest_v2.json");
                if (!File.Exists(path))
                {
                    LoadError = "缺少版本清单（内置资源与 data 目录都没有）";
                    return;
                }
                text = File.ReadAllText(path);
            }
            try
            {
                var ser = new JavaScriptSerializer();
                var root = (Dictionary<string, object>)ser.DeserializeObject(text);
                var arr = (object[])root["versions"];
                foreach (object o in arr)
                {
                    var d = (Dictionary<string, object>)o;
                    string type = Convert.ToString(d["type"]);
                    if (type != "release" && type != "snapshot") continue;
                    var v = new VersionInfo();
                    v.id = Convert.ToString(d["id"]);
                    v.type = type;
                    v.releaseTime = Convert.ToString(d["releaseTime"]);
                    v.url = Convert.ToString(d["url"]);
                    for (int i = 0; i < AprilIds.Length; i++)
                        if (AprilIds[i] == v.id) v.april = true;
                    items.Add(v);
                }
            }
            catch (Exception ex)
            {
                LoadError = "版本清单解析失败：" + ex.Message;
            }
        }

        public static VersionInfo Find(string id)
        {
            var list = Items;
            for (int i = 0; i < list.Count; i++)
                if (list[i].id == id) return list[i];
            return null;
        }

        public static string LatestRelease
        {
            get
            {
                var list = Items;
                for (int i = 0; i < list.Count; i++)
                    if (list[i].type == "release") return list[i].id;
                return "—";
            }
        }
    }

    public static class Downloader
    {
        const string UA = "McBotLauncher/1.0";

        public static string VersionDir(string id)
        {
            return Path.Combine(Path.Combine(Runtime.Dir, "downloads"), id);
        }

        /// <summary>手动跟随跳转（BMCLAPI 会 302 到 CDN，.NET 自动跟随偶尔解析失败）</summary>
        static HttpWebResponse Get(Uri uri, Action<string> log)
        {
            for (int hop = 0; hop < 8; hop++)
            {
                var req = (HttpWebRequest)WebRequest.Create(uri);
                req.AllowAutoRedirect = false;
                req.UserAgent = UA;
                req.Timeout = 30000;
                req.ReadWriteTimeout = 30000;
                var res = (HttpWebResponse)req.GetResponse();
                int code = (int)res.StatusCode;
                if (code >= 300 && code < 400)
                {
                    string loc = res.Headers["Location"];
                    res.Close();
                    if (string.IsNullOrEmpty(loc)) throw new Exception("跳转缺少 Location");
                    uri = new Uri(uri, loc);
                    if (log != null) log("跳转 → " + uri.Host);
                    continue;
                }
                return res;
            }
            throw new Exception("跳转次数过多");
        }

        public static string HttpGetText(string url)
        {
            using (var res = Get(new Uri(url), null))
            using (var sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        public static void DownloadFile(string url, string dest, Action<int, long, long> progress)
        {
            using (var res = Get(new Uri(url), null))
            {
                long total = res.ContentLength;
                long got = 0;
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                using (var input = res.GetResponseStream())
                using (var output = new FileStream(dest + ".part", FileMode.Create))
                {
                    var buf = new byte[81920];
                    int n;
                    while ((n = input.Read(buf, 0, buf.Length)) > 0)
                    {
                        output.Write(buf, 0, n);
                        got += n;
                        if (progress != null)
                        {
                            int pct = total > 0 ? (int)(got * 100 / total) : 0;
                            progress(pct, got, total);
                        }
                    }
                }
                File.Delete(dest);
                File.Move(dest + ".part", dest);
            }
        }

        static void DownloadCore(string id, string route, Action<int, string> report)
        {
            var info = VersionRepo.Find(id);
            if (info == null) throw new Exception("版本不存在：" + id);

            bool mirror = route == "bmclapi";
            string dir = VersionDir(id);
            Directory.CreateDirectory(dir);

            if (report != null) report(0, "解析版本 " + id + " …");
            string metaUrl = mirror
                ? "https://bmclapi2.bangbang93.com/version/" + id + "/json"
                : info.url;
            string meta = HttpGetText(metaUrl);
            File.WriteAllText(Path.Combine(dir, id + ".json"), meta, Encoding.UTF8);

            string jarUrl;
            if (mirror)
            {
                jarUrl = "https://bmclapi2.bangbang93.com/version/" + id + "/client";
            }
            else
            {
                var ser = new JavaScriptSerializer();
                var d = (Dictionary<string, object>)ser.DeserializeObject(meta);
                var downloads = (Dictionary<string, object>)d["downloads"];
                if (!downloads.ContainsKey("client")) throw new Exception("该版本没有 client 下载");
                var client = (Dictionary<string, object>)downloads["client"];
                jarUrl = Convert.ToString(client["url"]);
            }

            if (report != null) report(1, "下载 client.jar …");
            long lastPct = -1;
            DownloadFile(jarUrl, Path.Combine(dir, "client.jar"), delegate(int pct, long got, long total)
            {
                if (pct != lastPct && report != null)
                {
                    lastPct = pct;
                    string msg = string.Format("下载 client.jar {0}%（{1:0.0} MB / {2:0.0} MB）",
                        pct, got / 1048576.0, total / 1048576.0);
                    report(pct, msg);
                }
            });
        }

        /// <summary>下载（选镜像失败时自动回退官方）</summary>
        public static void Download(string id, string route, Action<int, string> report)
        {
            try
            {
                DownloadCore(id, route, report);
            }
            catch (Exception ex)
            {
                if (route != "bmclapi") throw;
                if (report != null)
                {
                    report(0, "第三方镜像失败（" + ex.Message + "），回退官方线路…");
                }
                DownloadCore(id, "official", report);
            }
        }
    }

    public class BotRunner
    {
        Process proc;
        StreamWriter stdin;
        public event Action<string, string> OnLog;
        public event Action<string, string> OnData;   // kind, json

        public bool Running
        {
            get { return proc != null && !proc.HasExited; }
        }

        public void Start(Config cfg, ServerEntry server)
        {
            StartOvo(cfg, server);
        }
        /// <summary>启动 OVO 引擎（内嵌的 DeepseekOVO 项目，用它自己的代码和 node）</summary>
        public void StartOvo(Config cfg, ServerEntry server)
        {
            if (!Runtime.OvoReady) throw new Exception("OVO 引擎还没解压好，请重启启动器");
            string dir = Runtime.OvoDir;

            // 0) 确保 OVO 源码带着启动器补丁（私信/公屏@ / 去网页 / 数据回传 / 背包）
            OvoPatcher.EnsurePatched(dir, null);
            // 0.05) 清空文件桥，避免读到上次会话的旧数据
            Runtime.ResetBridge();
            // 0.1) 写入当前的回复方式与回应范围
            Runtime.WriteReplyMode(cfg.bot != null ? cfg.bot.replyMode : "whisper");
            Runtime.WriteRespondScope(cfg.bot != null && cfg.bot.whitelistEnabled, cfg.bot != null ? cfg.bot.whitelist : "");

            // 1) keys.json：写入 API Key（保留已有其它键）
            string keysPath = Path.Combine(dir, "keys.json");
            var keys = new Dictionary<string, object>();
            try
            {
                var oldObj = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(keysPath));
                var od = oldObj as Dictionary<string, object>;
                if (od != null) keys = od;
            }
            catch { }
            if (cfg.ai.provider == "openai") keys["OPENAI_API_KEY"] = cfg.ai.apiKey;
            else keys["DEEPSEEK_API_KEY"] = cfg.ai.apiKey;
            File.WriteAllText(keysPath, new JavaScriptSerializer().Serialize(keys), new UTF8Encoding(false));

            // 2) settings.js：写服务器地址 / 版本，关掉自动开浏览器
            string settingsPath = Path.Combine(dir, "settings.js");
            string s = File.ReadAllText(settingsPath);
            s = Regex.Replace(s, "\"host\"\\s*:\\s*\"[^\"]*\"", "\"host\": \"" + server.host + "\"");
            s = Regex.Replace(s, "\"minecraft_version\"\\s*:\\s*\"[^\"]*\"", "\"minecraft_version\": \"" + server.version + "\"");
            s = Regex.Replace(s, "\"port\"\\s*:\\s*\\d+", "\"port\": " + server.port);
            s = Regex.Replace(s, "\"auto_open_ui\"\\s*:\\s*(true|false)", "\"auto_open_ui\": false");
            File.WriteAllText(settingsPath, s, new UTF8Encoding(false));

            // 3) 角色名写进默认 profile
            string prof = Path.Combine(dir, "andy.json");
            try
            {
                if (File.Exists(prof))
                {
                    var p = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(prof)) as Dictionary<string, object>;
                    if (p != null)
                    {
                        p["name"] = cfg.character.username;
                        File.WriteAllText(prof, new JavaScriptSerializer().Serialize(p), new UTF8Encoding(false));
                    }
                }
            }
            catch { }

            // 4) 起 main.js（端口走环境变量，不弹交互）
            var psi = new ProcessStartInfo();
            psi.FileName = Runtime.OvoNode;
            psi.Arguments = "main.js";
            psi.WorkingDirectory = dir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
            psi.EnvironmentVariables["MINECRAFT_PORT"] = server.port.ToString();

            proc = new Process();
            proc.StartInfo = psi;
            proc.OutputDataReceived += delegate(object s2, DataReceivedEventArgs e) { Line(e.Data); };
            proc.ErrorDataReceived += delegate(object s2, DataReceivedEventArgs e) { Line(e.Data); };
            proc.EnableRaisingEvents = true;
            proc.Exited += delegate
            {
                if (OnLog != null) OnLog("warn", "OVO 进程已退出");
            };
            proc.Start();
            try
            {
                stdin = new StreamWriter(proc.StandardInput.BaseStream, new UTF8Encoding(false));
                stdin.AutoFlush = true;
            }
            catch { stdin = null; }
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
        }

        /// <summary>给机器人进程发命令（一行 JSON）</summary>
        public void Send(string json)
        {
            try
            {
                if (stdin != null) { stdin.WriteLine(json); stdin.Flush(); return; }
                if (proc != null && !proc.HasExited) proc.StandardInput.WriteLine(json);
            }
            catch { }
        }

        void Line(string raw)
        {
            if (raw == null) return;

            if (raw.StartsWith("DATA|"))
            {
                string[] dp = raw.Split(new char[] { '|' }, 3);
                if (dp.Length == 3 && OnData != null) OnData(dp[1], dp[2]);
                return;
            }

            string level = "info";
            string msg = raw;
            if (raw.StartsWith("LOG|"))
            {
                string[] parts = raw.Split(new char[] { '|' }, 3);
                if (parts.Length == 3) { level = parts[1]; msg = parts[2]; }
            }
            if (OnLog != null) OnLog(level, msg);
        }

        public void Stop()
        {
            try { if (stdin != null) { stdin.Close(); stdin = null; } } catch { }
            try
            {
                if (proc != null && !proc.HasExited)
                {
                    proc.Kill();
                    proc.WaitForExit(2000);
                }
            }
            catch { }
            proc = null;
        }
    }
}
