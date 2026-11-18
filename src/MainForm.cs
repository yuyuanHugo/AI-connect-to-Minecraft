using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace McBot
{
    public class MainForm : Form
    {
        public Config Cfg;
        ConfigPage configPage;
        LaunchPage launchPage;
        StatusPage statusPage;
        System.Windows.Forms.Timer bridgeTimer;
        long bridgeOffset;
        string bridgeTail = "";
        Segmented tabs;
        BotRunner bot = new BotRunner();

        public MainForm()
        {
            Text = "MineBot AI · Minecraft AI Bot 启动器";
            ClientSize = new Size(1180, 700);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = T.Bg;
            DoubleBuffered = true;
            Font = T.FBase;

            tabs = new Segmented();
            tabs.Items = new string[] { "配置", "启动", "状态" };
            tabs.Font = T.FSmall;
            tabs.SetBounds(300, 16, 252, 32);
            tabs.BackColor = T.BgTop;
            tabs.Changed += delegate { SwitchPage(tabs.SelectedIndex); };
            Controls.Add(tabs);

            configPage = new ConfigPage();
            configPage.SetBounds(0, 64, 1180, 636);
            Controls.Add(configPage);

            launchPage = new LaunchPage();
            launchPage.SetBounds(0, 64, 1180, 636);
            launchPage.Visible = false;
            Controls.Add(launchPage);

            statusPage = new StatusPage();
            statusPage.SetBounds(0, 64, 1180, 636);
            statusPage.Visible = false;
            Controls.Add(statusPage);

            // 配置页事件
            configPage.ConfigChanged += delegate { RefreshLaunch(); Invalidate(); };
            configPage.AddServer += delegate { AddOrEditServer(null); };
            configPage.EditServer += delegate(object s, EventArgs e) { AddOrEditServer((ServerEntry)s); };

            // 启动页事件
            launchPage.ServerChanged += delegate
            {
                configPage.RefreshServers();
                RefreshLaunch();
                Invalidate();
            };
            launchPage.LaunchClicked += delegate { DoLaunch(); };
            launchPage.StopClicked += delegate { DoStop(); };

            // 状态页事件
            statusPage.ReplyModeChanged += delegate
            {
                if (Cfg.bot == null) Cfg.bot = new BotConfig();
                Cfg.bot.replyMode = statusPage.PublicReply ? "public" : "whisper";
                Store.Save(Cfg);
                Runtime.WriteReplyMode(Cfg.bot.replyMode);
                AppendLog("info", statusPage.PublicReply
                    ? "回复方式：公屏 @（只有被点名才回应）"
                    : "回复方式：私信（悄悄话）");
            };            statusPage.ScopeChanged += delegate
            {
                if (Cfg.bot == null) Cfg.bot = new BotConfig();
                Cfg.bot.whitelistEnabled = statusPage.WhitelistOnly;
                Cfg.bot.whitelist = statusPage.WhitelistText;
                Store.Save(Cfg);
                Runtime.WriteRespondScope(Cfg.bot.whitelistEnabled, Cfg.bot.whitelist);
                if (Cfg.bot.whitelistEnabled)
                    AppendLog("info", "回应范围：仅白名单（" + (Cfg.bot.whitelist.Length == 0 ? "名单为空，暂时谁都不回" : Cfg.bot.whitelist) + "）");
                else
                    AppendLog("info", "回应范围：所有人");
            };            statusPage.RestartBot += delegate { DoStop(); DoLaunch(); };
            statusPage.DisconnectBot += delegate { DoStop(); };

            bot.OnLog += delegate(string level, string msg)
            {
                if (!IsHandleCreated) return;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        launchPage.Log.Append(level, msg);
                        if (msg.IndexOf("已登录") >= 0) { launchPage.SetRunning(true); statusPage.SetRunning(true); }
                    });
                }
                catch { }
            };

            bot.OnData += delegate(string kind, string json)
            {
                if (!IsHandleCreated) return;
                try
                {
                    BeginInvoke((MethodInvoker)delegate { statusPage.HandleData(kind, json); });
                }
                catch { }
            };


            Cfg = Store.Load();
            configPage.Bind(Cfg);
            launchPage.Bind(Cfg);
            statusPage.Refresh(Cfg);
            statusPage.SetRunning(false);

            // 文件桥：OVO 把 DATA| 行写进 bridge.log，这里轮询读取（不依赖 stdout）
            bridgeTimer = new System.Windows.Forms.Timer();
            bridgeTimer.Interval = 600;
            bridgeTimer.Tick += delegate { PumpBridge(); };
            bridgeTimer.Start();
            PumpBridge();   // 启动时先把已有的数据读进来
            AppendLog("info", "启动器就绪 · 当前版本 " + Cfg.client.version);

            // 首次运行时把内置的 Node 运行时解压到配置目录
            if (!Runtime.OvoReady)
            {
                runtimeBusy = true;
                var t = new Thread(delegate()
                {
                    bool ok = Runtime.EnsureOvo(delegate(string m)
                    {
                        try { BeginInvoke((MethodInvoker)delegate { AppendLog("info", m); }); }
                        catch { }
                    });
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            runtimeBusy = false;
                            Invalidate();
                            AppendLog(ok ? "ok" : "error", ok ? "机器人运行时已就绪，可以启动" : "机器人运行时准备失败");
                        });
                    }
                    catch { }
                });
                t.IsBackground = true;
                t.Start();
            }
            else
            {
                AppendLog("ok", "机器人运行时已就绪");
            }

            LayoutWindow();
        }

        bool runtimeBusy;

        public void ShowLaunchPage()
        {
            tabs.SelectedIndex = 1;
            SwitchPage(1);
        }

        void LayoutWindow()
        {
            if (configPage == null || launchPage == null) return;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            int pageH = Math.Max(120, h - 64);
            configPage.SetBounds(0, 64, w, pageH);
            launchPage.SetBounds(0, 64, w, pageH);
            if (statusPage != null) statusPage.SetBounds(0, 64, w, pageH);

            if (tabs != null)
            {
                int tw = 252;
                int tx = (w - tw) / 2;
                if (tx < 230) tx = 230;
                if (tx + tw > w - 240) tx = Math.Max(230, w - 240 - tw);
                tabs.SetBounds(tx, 16, tw, 32);
            }

            configPage.LayoutPage();
            launchPage.LayoutPage();
            if (statusPage != null) statusPage.LayoutPage();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutWindow();
        }

        public void SetForceTip(bool v)
        {
            launchPage.ForceTip = v;
            launchPage.Invalidate();
        }

        public void ShowStatusPage()
        {
            tabs.SelectedIndex = 2;
            SwitchPage(2);
        }

        public void ShowPage(int idx)
        {
            if (idx < 0 || idx > 2) return;
            tabs.SelectedIndex = idx;
            SwitchPage(idx);
        }

        void SwitchPage(int idx)
        {
            configPage.Visible = idx == 0;
            launchPage.Visible = idx == 1;
            statusPage.Visible = idx == 2;
            if (idx == 2)
            {
                statusPage.Refresh(Cfg);
                statusPage.LayoutPage();
            }
        }

        /// <summary>增量读取 OVO 的 bridge.log（DATA|kind|json），喂给状态页</summary>
        void PumpBridge()
        {
            try
            {
                string path = Runtime.BridgeLog;
                if (!File.Exists(path)) return;
                long len = new FileInfo(path).Length;
                if (len < bridgeOffset) { bridgeOffset = 0; bridgeTail = ""; }
                if (len == bridgeOffset) return;

                string text;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(bridgeOffset, SeekOrigin.Begin);
                    var buf = new byte[len - bridgeOffset];
                    int read = fs.Read(buf, 0, buf.Length);
                    bridgeOffset += read;
                    text = Encoding.UTF8.GetString(buf, 0, read);
                }

                text = bridgeTail + text;
                int lastNl = text.LastIndexOf('\n');
                if (lastNl < 0) { bridgeTail = text; return; }
                bridgeTail = text.Substring(lastNl + 1);

                foreach (string rawLine in text.Substring(0, lastNl).Split('\n'))
                {
                    string line = rawLine.TrimEnd('\r');
                    if (!line.StartsWith("DATA|")) continue;
                    string[] parts = line.Split(new char[] { '|' }, 3);
                    if (parts.Length == 3) statusPage.HandleData(parts[1], parts[2]);
                }
            }
            catch { }
        }
        void RefreshLaunch()
        {
            launchPage.Bind(Cfg);
        }

        // ------------------------------------------------ 服务器
        void AddOrEditServer(ServerEntry target)
        {
            var current = Cfg.SelectedServer;
            string seedVersion = current != null ? current.version : Cfg.client.version;
            if (target != null) seedVersion = target.version;

            var dlg = new ServerDialog(target, seedVersion);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            if (target == null)
            {
                dlg.Result.id = Store.NewId();
                Cfg.servers.Add(dlg.Result);
                if (Cfg.selectedServerId == null) Cfg.selectedServerId = dlg.Result.id;
            }
            else
            {
                int i = Cfg.servers.FindIndex(delegate(ServerEntry x) { return x.id == target.id; });
                if (i >= 0)
                {
                    dlg.Result.id = target.id;
                    Cfg.servers[i] = dlg.Result;
                }
            }
            // 选中服务器的版本即启动版本
            configPage.SyncVersionToServer();
            Store.Save(Cfg);
            configPage.RefreshServers();
            RefreshLaunch();
            Invalidate();
        }

        // ------------------------------------------------ 启动
        void DoLaunch()
        {
            var srv = Cfg.SelectedServer;
            if (srv == null) { AppendLog("warn", "还没有选择服务器"); return; }
            if (srv.version != Cfg.client.version)
            {
                AppendLog("warn", "版本不一致，无法启动");
                return;
            }

            // 第一次用：先解压 OVO（940 MB，两三分钟），解完再启动
            if (!Runtime.OvoReady)
            {
                statusPage.SetRunning(false);

            // 文件桥：OVO 把 DATA| 行写进 bridge.log，这里轮询读取（不依赖 stdout）
            bridgeTimer = new System.Windows.Forms.Timer();
            bridgeTimer.Interval = 600;
            bridgeTimer.Tick += delegate { PumpBridge(); };
            bridgeTimer.Start();
            PumpBridge();   // 启动时先把已有的数据读进来
                AppendLog("info", "首次使用 OVO 引擎，正在解压（约 940 MB，请稍候）…");
                var t = new Thread(delegate()
                {
                    bool ok = Runtime.EnsureOvo(delegate(string m)
                    {
                        try { BeginInvoke((MethodInvoker)delegate { AppendLog("info", m); }); }
                        catch { }
                    });
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (!ok) { AppendLog("error", "OVO 引擎解压失败"); return; }
                            AppendLog("ok", "OVO 引擎就绪，开始启动");
                            ActuallyLaunch(srv);
                        });
                    }
                    catch { }
                });
                t.IsBackground = true;
                t.Start();
                return;
            }

            ActuallyLaunch(srv);
        }

        void ActuallyLaunch(ServerEntry srv)
        {
            try
            {
                AppendLog("info", string.Format("正在连接 {0}:{1}（版本 {2}）", srv.host, srv.port, srv.version));
                bot.Start(Cfg, srv);
                launchPage.SetRunning(true);
                statusPage.Refresh(Cfg);
                statusPage.SetRunning(true);
                statusPage.AppendSystem("已启动，正在连接 " + srv.host + ":" + srv.port);
                AppendLog("ok", "机器人进程已启动");
            }
            catch (Exception ex)
            {
                AppendLog("error", "启动失败：" + ex.Message);
            }
        }

        void DoStop()
        {
            bot.Stop();
            launchPage.SetRunning(false);
            statusPage.SetRunning(false);

            // 文件桥：OVO 把 DATA| 行写进 bridge.log，这里轮询读取（不依赖 stdout）
            bridgeTimer = new System.Windows.Forms.Timer();
            bridgeTimer.Interval = 600;
            bridgeTimer.Tick += delegate { PumpBridge(); };
            bridgeTimer.Start();
            PumpBridge();   // 启动时先把已有的数据读进来
            AppendLog("info", "已停止机器人");
        }

        public void AppendLog(string level, string msg)
        {
            launchPage.Log.Append(level, msg);
        }

        public void FillDemoLogs()
        {
            string[,] lines = new string[,] {
                {"info", "正在连接 mc.minebot.dev:25565（版本 1.21.4）…"},
                {"ok",   "已登录：Steve_AI"},
                {"info", "已进入世界，坐标 128.5, 64.0, -302.4"},
                {"chat", "<Alex_233> Steve_AI 跟我去挖矿吗"},
                {"ai",   "AI 调用工具 → follow({\"player\":\"Alex_233\",\"distance\":3})"},
                {"ai",   "回复 Alex_233：好，我跟着你，别跑太快"},
                {"chat", "<Steve_AI> 好，我跟着你，别跑太快"},
                {"info", "寻路中：目标 Alex_233（距离 3）"},
                {"warn", "检测到前方 2 格高的障碍，自动绕行"},
                {"ok",   "已抵达目标附近，距离 2.8 格"},
                {"chat", "<Alex_233> 那边有个洞穴"},
                {"ai",   "AI 调用工具 → goto({\"x\":131,\"y\":58,\"z\":-309})"},
                {"info", "寻路中：目标 (131, 58, -309)"},
                {"ok",   "到达目标坐标，用时 6.4s"},
                {"chat", "<Alex_233> 帅"}
            };
            for (int i = 0; i < lines.GetLength(0); i++)
                launchPage.Log.Append(lines[i, 0], lines[i, 1]);
        }

        public bool DepsInstalled
        {
            get { return Runtime.OvoReady; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var br = new SolidBrush(T.Bg))
                g.FillRectangle(br, ClientRectangle);

            using (var br = new SolidBrush(T.BgTop))
                g.FillRectangle(br, 0, 0, Width, 64);
            using (var p = new Pen(T.Line, 1f))
                g.DrawLine(p, 0, 64, Width, 64);

            var lr = new Rectangle(26, 14, 36, 36);
            T.FillGrad(g, lr, 10, T.AccentLight, T.Accent);
            using (var b = new SolidBrush(T.OnAccent))
            {
                g.FillRectangle(b, 34, 22, 7, 7);
                g.FillRectangle(b, 45, 22, 7, 7);
                g.FillRectangle(b, 34, 33, 7, 7);
                g.FillRectangle(b, 45, 33, 7, 7);
            }
            T.TextAt(g, "MineBot AI", T.FTitle, T.Text, 74, 14);
            T.TextAt(g, "Minecraft AI Bot 启动器", T.FSmall, T.Muted, 74, 34);

            // 右侧状态
            string verText = "版本 " + Cfg.client.version;
            int vw = T.MeasureBadge(g, verText, T.FSmall);
            int vx = Width - 26 - vw;
            T.Badge(g, verText, T.FSmall, T.Dim, T.CardSoft, T.Line, vx, 21);

            string depText = runtimeBusy ? "运行时准备中" : (DepsInstalled ? "运行时就绪" : "运行时未就绪");
            int dw = T.MeasureBadge(g, depText, T.FSmall);
            int dx = vx - 10 - dw;
            if (DepsInstalled)
                T.Badge(g, depText, T.FSmall, T.Ok, T.OkSoft, Color.FromArgb(190, 226, 210), dx, 21);
            else
                T.Badge(g, depText, T.FSmall, T.Warn, T.WarnSoft, Color.FromArgb(238, 214, 170), dx, 21);
        }
    }

    // ---------------------------------------------------------------- 服务器编辑
    public class ServerDialog : Form
    {
        public ServerEntry Result = new ServerEntry();
        InputBox host, port, note;
        Label verLbl;
        string version;

        public ServerDialog(ServerEntry target, string fallbackVersion)
        {
            Text = target == null ? "添加服务器" : "编辑服务器";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 330);
            BackColor = T.Card;
            ShowInTaskbar = false;

            var title = new Label();
            title.Text = Text;
            title.Font = T.FCardTitle;
            title.ForeColor = T.Text;
            title.SetBounds(18, 16, 240, 22);
            Controls.Add(title);

            AddLabel("地址", 56);
            host = new InputBox();
            host.SetMono(true);
            host.SetBounds(110, 52, 290, 32);
            Controls.Add(host);

            AddLabel("端口", 98);
            port = new InputBox();
            port.SetMono(true);
            port.SetBounds(110, 94, 290, 32);
            Controls.Add(port);

            AddLabel("版本", 140);
            var verBtn = new FlatButton();
            verBtn.Text = "";
            verBtn.SetBounds(110, 136, 290, 32);
            verBtn.Click += delegate
            {
                var dlg = new VersionDialog(version);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    version = dlg.SelectedVersion;
                    verLbl.Text = version;
                }
            };
            Controls.Add(verBtn);

            verLbl = new Label();
            verLbl.Text = fallbackVersion;
            verLbl.Font = T.FMono;
            verLbl.ForeColor = T.Text;
            verLbl.BackColor = Color.Transparent;
            verLbl.SetBounds(122, 143, 260, 20);
            Controls.Add(verLbl);
            verLbl.BringToFront();

            AddLabel("备注", 182);
            note = new InputBox();
            note.SetBounds(110, 178, 290, 32);
            Controls.Add(note);

            var cancel = new FlatButton();
            cancel.Text = "取消";
            cancel.SetBounds(196, 274, 96, 34);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var ok = new FlatButton();
            ok.Text = "保存";
            ok.Kind = BtnKind.Primary;
            ok.SetBounds(304, 274, 96, 34);
            ok.Click += delegate
            {
                Result.host = host.Text.Trim();
                int p = 25565;
                int.TryParse(port.Text.Trim(), out p);
                if (p <= 0) p = 25565;
                Result.port = p;
                Result.version = version;
                Result.note = note.Text.Trim();
                if (Result.host.Length == 0)
                {
                    MessageBox.Show("请填写服务器地址", "提示");
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            version = target != null ? target.version : fallbackVersion;
            verLbl.Text = version;
            if (target != null)
            {
                host.Text = target.host;
                port.Text = target.port.ToString();
                note.Text = target.note;
            }
            else
            {
                port.Text = "25565";
            }
        }

        void AddLabel(string text, int y)
        {
            var l = new Label();
            l.Text = text;
            l.Font = T.FSmall;
            l.ForeColor = T.Dim;
            l.SetBounds(18, y + 6, 88, 20);
            Controls.Add(l);
        }
    }
}
