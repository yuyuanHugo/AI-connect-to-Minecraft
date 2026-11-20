using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace McBot
{
    /// <summary>
    /// 单文件运行时：node.exe + bot（agent.js 与 node_modules）全部作为资源内嵌，
    /// 首次运行时解压到配置目录下。
    /// </summary>
    public static class Runtime
    {
        const string ManifestResource = "McBot.version_manifest.json";
        const string SkinSteveResource = "McBot.steve.png";
        const string SkinAlexResource = "McBot.alex.png";

        static string dirCache;

        /// <summary>数据目录：优先 exe 同级的 MineBotData（便携），其次 LocalAppData，最后临时目录</summary>
        public static string Dir
        {
            get
            {
                if (dirCache != null) return dirCache;
                dirCache = ResolveDir();
                return dirCache;
            }
        }

        static string ResolveDir()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(exeDir))
            {
                string portable = Path.Combine(exeDir, "MineBotData");
                if (TryUse(portable)) return portable;
            }

            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(baseDir)) baseDir = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(baseDir))
            {
                string primary = Path.Combine(baseDir, "MineBot");
                if (TryUse(primary)) return primary;
            }

            string fallback = Path.Combine(Path.GetTempPath(), "MineBot");
            try { Directory.CreateDirectory(fallback); } catch { }
            return fallback;
        }

        static bool TryUse(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".writable");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }


        /// <summary>超长路径加 \\?\ 前缀（Windows 260 字符限制）</summary>
        public static string Long(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length < 240) return path;
            if (path.StartsWith(@"\\?\")) return path;
            if (path.StartsWith(@"\\")) return @"\\?\UNC\" + path.Substring(2);
            return @"\\?\" + path;
        }

        static void Trace(string line)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.AppendAllText(Path.Combine(Dir, "extract.log"),
                    DateTime.Now.ToString("HH:mm:ss") + "  " + line + Environment.NewLine);
            }
            catch { }
        }

        /// <summary>启动诊断日志（临时目录）</summary>
        public static string BootLog
        {
            get { return Path.Combine(Path.GetTempPath(), "mcbot-boot.log"); }
        }

        public static void Boot(string line)
        {
            try
            {
                File.AppendAllText(BootLog,
                    DateTime.Now.ToString("HH:mm:ss") + "  " + line + Environment.NewLine);
            }
            catch { }
        }

        // ---------------- OVO 引擎（内嵌整个 DeepseekOVO 项目） ----------------
        public static string OvoDir { get { return Path.Combine(Dir, "ovo"); } }
        public static string OvoStart { get { return Path.Combine(OvoDir, "main.js"); } }
        public static string OvoNode
        {
            get
            {
                string n = Path.Combine(OvoDir, "runtime\\node.exe");
                return File.Exists(n) ? n : "node.exe";
            }
        }
        static string OvoMarker { get { return Path.Combine(Dir, ".ovo-2"); } }

        /// <summary>写回应范围：所有人 / 仅白名单（OVO 每条消息前读取，改完即时生效）</summary>
        public static void WriteRespondScope(bool whitelistOnly, string names)
        {
            try
            {
                Directory.CreateDirectory(OvoDir);
                var utf8 = new System.Text.UTF8Encoding(false);
                File.WriteAllText(Path.Combine(OvoDir, "respond-scope.txt"), whitelistOnly ? "whitelist" : "all", utf8);
                File.WriteAllText(Path.Combine(OvoDir, "whitelist.txt"), names == null ? "" : names, utf8);
            }
            catch { }
        }
        /// <summary>写昵称（她自称的名字），OVO 组提示词时读取</summary>
        public static void WritePersona(string nickname)
        {
            try
            {
                Directory.CreateDirectory(OvoDir);
                File.WriteAllText(Path.Combine(OvoDir, "persona-name.txt"), nickname == null ? "" : nickname.Trim(), new System.Text.UTF8Encoding(false));
            }
            catch { }
        }

        /// <summary>OVO 文件桥日志（DATA| 行）</summary>
        public static string BridgeLog
        {
            get { return Path.Combine(OvoDir, "bridge.log"); }
        }

        /// <summary>启动前清空文件桥</summary>
        public static void ResetBridge()
        {
            try
            {
                Directory.CreateDirectory(OvoDir);
                File.WriteAllText(BridgeLog, "");
            }
            catch { }
        }
        /// <summary>写回复方式（whisper / public），OVO 每次回复前读取，切换即时生效</summary>
        public static void WriteReplyMode(string mode)
        {
            try
            {
                string m = (mode == "public") ? "public" : "whisper";
                Directory.CreateDirectory(OvoDir);
                File.WriteAllText(Path.Combine(OvoDir, "reply-mode.txt"), m, new System.Text.UTF8Encoding(false));
            }
            catch { }
        }
        public static bool OvoReady
        {
            get { return File.Exists(OvoMarker) && File.Exists(OvoStart); }
        }

        /// <summary>解压内嵌的 OVO 项目（8 万多个文件，用系统 tar 批量解，比逐文件快一个数量级）</summary>
        public static bool EnsureOvo(Action<string> log)
        {
            if (OvoReady) return true;
            if (log != null) log("正在解压 OVO 引擎（约 940 MB / 8 万个文件，请稍候）…");

            string tmp = Path.Combine(Dir, "ovo.tmp.zip");
            try
            {
                // 1) 把内嵌 zip 落盘
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream("McBot.ovo.zip"))
                {
                    if (s == null)
                    {
                        Trace("ovo resource missing");
                        if (log != null) log("OVO 资源缺失（编译时未打包）");
                        return false;
                    }
                    if (File.Exists(tmp)) File.Delete(tmp);
                    using (var f = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                        s.CopyTo(f, 1 << 20);
                }
                Trace("ovo zip written " + new FileInfo(tmp).Length);

                // 2) 用系统 tar 解压（bsdtar 支持 zip）
                if (Directory.Exists(OvoDir))
                {
                    try { Directory.Delete(OvoDir, true); }
                    catch (Exception e) { Trace("ovo delete failed, overwrite: " + e.Message); }
                }
                Directory.CreateDirectory(OvoDir);

                string tar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
                if (File.Exists(tar))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = tar;
                    psi.Arguments = "-xf \"" + tmp + "\" -C \"" + OvoDir + "\"";
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardError = true;
                    var p = System.Diagnostics.Process.Start(psi);
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    Trace("tar exit=" + p.ExitCode + " err=" + err);
                }

                // 3) tar 不可用或失败 → 退回逐文件解压
                if (!File.Exists(Path.Combine(OvoDir, "main.js")))
                {
                    Trace("tar failed, fallback to managed extract");
                    if (!ExtractFrom("McBot.ovo.zip", OvoDir, log, "OVO")) return false;
                }

                File.WriteAllText(OvoMarker, DateTime.Now.ToString("s"));
                Trace("ovo marker written, ready=" + OvoReady);
                if (log != null) log("OVO 引擎就绪");
                return OvoReady;
            }
            catch (Exception ex)
            {
                Trace("EnsureOvo FAILED: " + ex.ToString());
                if (log != null) log("OVO 解压失败：" + ex.Message);
                return false;
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        /// <summary>从内嵌资源解压一个 zip 到目标目录（OVO 等大包用）</summary>
        static bool ExtractFrom(string resource, string destDir, Action<string> log, string label)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(resource))
                {
                    if (s == null)
                    {
                        Trace("resource missing: " + resource);
                        if (log != null) log(label + " 资源缺失（编译时未打包）");
                        return false;
                    }
                    Trace(label + " payload bytes=" + s.Length);

                    var payload = new byte[s.Length];
                    int read = 0;
                    while (read < payload.Length)
                    {
                        int n = s.Read(payload, read, payload.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }

                    if (Directory.Exists(Long(destDir)))
                    {
                        try { Directory.Delete(Long(destDir), true); }
                        catch (Exception delEx) { Trace("delete failed, overwrite mode: " + delEx.Message); }
                    }
                    Directory.CreateDirectory(Long(destDir));

                    using (var ms = new MemoryStream(payload))
                    using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
                    {
                        int count = 0;
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            string name = entry.FullName;
                            bool isDir = name.EndsWith("/") || name.EndsWith("\\");
                            string rel = name.Replace('/', '\\').TrimEnd('\\');
                            if (rel.Length == 0) continue;
                            string dest = Path.Combine(destDir, rel);
                            if (isDir) { Directory.CreateDirectory(Long(dest)); continue; }
                            string parent = Path.GetDirectoryName(dest);
                            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(Long(parent));
                            using (Stream es = entry.Open())
                            using (var fs = new FileStream(Long(dest), FileMode.Create, FileAccess.Write))
                                es.CopyTo(fs, 65536);
                            count++;
                        }
                        Trace(label + " extracted files=" + count);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace("extract " + label + " FAILED: " + ex.ToString());
                if (log != null) log(label + " 解压失败：" + ex.Message);
                return false;
            }
        }

        public static byte[] ReadResource(string name)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(name))
                {
                    if (s == null) return null;
                    var buf = new byte[s.Length];
                    int off = 0;
                    while (off < buf.Length)
                    {
                        int n = s.Read(buf, off, buf.Length - off);
                        if (n <= 0) break;
                        off += n;
                    }
                    return buf;
                }
            }
            catch { return null; }
        }

        public static string ReadManifestText()
        {
            byte[] b = ReadResource(ManifestResource);
            if (b == null) return null;
            return System.Text.Encoding.UTF8.GetString(b);
        }

        public static byte[] SkinBytes(string model)
        {
            return ReadResource(model == "alex" ? SkinAlexResource : SkinSteveResource);
        }
    }
}
