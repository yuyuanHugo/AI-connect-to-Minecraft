using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace McBot
{
    // ---------------------------------------------------------------- 小控件
    public class VersionButton : Control
    {
        public string Version = "1.21.4";
        public string Badge = "正式版";
        public bool April = false;

        public VersionButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            T.Fill(g, r, 8, T.Input);
            T.Stroke(g, r, 8, T.LineStrong);
            T.TextAt(g, Version, T.FMono, T.Text, 12, 0, Width - 60, Height, StringAlignment.Near);

            Color fg = April ? T.Amber : (Badge == "快照" ? T.Blue : T.Green);
            Color bg = Color.FromArgb(28, fg.R, fg.G, fg.B);
            int bx = 12 + (int)g.MeasureString(Version, T.FMono).Width + 10;
            T.Badge(g, Badge, T.FSmall, fg, bg, Color.FromArgb(90, fg.R, fg.G, fg.B), bx, (Height - 20) / 2);

            using (var p = new Pen(T.Muted, 1.6f))
            {
                int cx = Width - 18, cy = Height / 2;
                g.DrawLines(p, new Point[] {
                    new Point(cx - 4, cy - 2), new Point(cx, cy + 3), new Point(cx + 4, cy - 2)
                });
            }
        }
    }

    public class ProgressLite : Control
    {
        int val = 0;
        public int Value
        {
            get { return val; }
            set { val = value; if (val < 0) val = 0; if (val > 100) val = 100; Invalidate(); }
        }

        public ProgressLite()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Height = 7;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            T.Fill(g, r, 4, T.Input);
            T.Stroke(g, r, 4, T.Line);
            int w = (int)((Width - 2) * val / 100.0);
            if (w > 3) T.Fill(g, new Rectangle(1, 1, w, Height - 2), 3, T.GreenDeep);
        }
    }

    public class InfoRow : Control
    {
        public string Key = "";
        public string Val = "";
        public bool Clickable = false;
        public bool Chevron = false;
        bool hover;

        public InfoRow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Height = 40;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (Clickable && hover) T.Fill(g, new Rectangle(4, 2, Width - 8, Height - 4), 6, T.Hover);
            T.TextAt(g, Key, T.FSmall, T.Muted, 8, 0, 84, Height, StringAlignment.Near);
            Color vc = Clickable && hover ? T.Green : T.Text;
            T.TextAt(g, Val, T.FSmall, vc, 96, 0, Width - (Chevron ? 130 : 108), Height, StringAlignment.Near);
            if (Chevron)
            {
                using (var p = new Pen(T.Muted, 1.6f))
                {
                    int cx = Width - 16, cy = Height / 2;
                    g.DrawLines(p, new Point[] {
                        new Point(cx - 2, cy - 4), new Point(cx + 3, cy), new Point(cx - 2, cy + 4)
                    });
                }
            }
            using (var p = new Pen(T.Line, 1f))
                g.DrawLine(p, 8, Height - 1, Width - 8, Height - 1);
        }
    }

    // ---------------------------------------------------------------- 配置页
    public class ConfigPage : Panel
    {
        public Config Cfg;
        public event EventHandler ConfigChanged;
        public event EventHandler SkinPicked;

        Card cardAi, cardChar, cardSrv;
        InputBox Nickname;
        FlatButton PromptBtn;
        Label promptInfo;
        public SelectBox Provider;
        public InputBox Model, BaseUrl, ApiKey, Username, SkinPath;
        public Slider Temp;
        Label tempVal;
        public Segmented ModelSeg;
        public AvatarView Preview;
        public ServerTable Table;
        FlatButton browseBtn, addBtn, keyBtn;

        public EventHandler AddServer;
        public EventHandler EditServer;

        public ConfigPage()
        {
            BackColor = T.Bg;
            BuildAiCard();
            BuildCharCard();
            BuildServerCard();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutPage();
        }

        Label Lbl(Control parent, string text, int x, int y, int w)
        {
            var l = new Label();
            l.Text = text;
            l.Font = T.FSmall;
            l.ForeColor = T.Dim;
            l.AutoSize = false;
            l.BackColor = Color.Transparent;
            l.SetBounds(x, y + 6, w, 20);
            parent.Controls.Add(l);
            return l;
        }

        /// <summary>自适应布局：窗口尺寸变化时重排卡片与控件</summary>
        public void LayoutPage()
        {
            if (cardAi == null || cardChar == null || cardSrv == null) return;
            int m = 24, gap = 16;
            int usable = Width - m * 2;
            if (usable < 620) return;

            int colW = (usable - gap) / 2;
            cardAi.SetBounds(m, 12, colW, 336);
            cardChar.SetBounds(m + colW + gap, 12, colW, 336);
            LayoutAiCard(colW);
            LayoutCharCard(colW);

            int srvY = 360;
            int avail = Height - srvY - 16;
            // 服务器卡片铺满剩余高度（内容超出时表格可滚轮滚动）
            int cardH = Math.Max(180, avail);

            cardSrv.SetBounds(m, srvY, usable, cardH);
            addBtn.SetBounds(usable - 16 - 132, 20, 132, 30);
            Table.SetBounds(12, 56, usable - 24, Math.Max(60, cardH - 88));
        }

        void LayoutAiCard(int w)
        {
            int ix = 112;
            int iw = w - ix - 16;
            Provider.SetBounds(ix, 54, Math.Min(220, iw), 32);
            Model.SetBounds(ix, 94, iw, 32);
            BaseUrl.SetBounds(ix, 134, iw, 32);

            const int kb = 46;
            ApiKey.SetBounds(ix, 174, iw - kb - 8, 32);
            keyBtn.SetBounds(ix + iw - kb, 174, kb, 32);

            Temp.SetBounds(ix, 214, Math.Max(80, iw - 60), 24);
            tempVal.SetBounds(ix + iw - 48, 216, 40, 20);

            Nickname.SetBounds(ix, 254, iw, 32);
            PromptBtn.SetBounds(ix, 294, 132, 32);
            promptInfo.SetBounds(ix + 140, 294, Math.Max(60, iw - 140), 32);
        }

        void LayoutCharCard(int w)
        {
            int ix = 112;
            int iw = w - ix - 16;
            const int bw = 74;
            Username.SetBounds(ix, 54, iw, 32);
            SkinPath.SetBounds(ix, 94, Math.Max(80, iw - bw - 8), 32);
            browseBtn.SetBounds(ix + iw - bw, 94, bw, 32);
            ModelSeg.SetBounds(ix, 140, Math.Min(200, iw), 34);
            Preview.SetBounds(w - 16 - 76, 132, 76, 76);
        }

        void BuildAiCard()
        {
            cardAi = new Card();
            cardAi.Title = "AI 设置";
            cardAi.Subtitle = "模型接口来自 OpenAI 兼容协议";
            cardAi.Accent = T.Violet;
            cardAi.SetBounds(24, 12, 552, 336);
            Controls.Add(cardAi);

            Lbl(cardAi, "提供商", 18, 54, 88);
            Provider = new SelectBox();
            Provider.Items = new string[] { "OpenAI", "DeepSeek", "Moonshot", "自定义 / 本地" };
            Provider.Font = T.FBase;
            Provider.SetBounds(112, 54, 220, 32);
            Provider.Changed += delegate { OnAiChanged(); };
            cardAi.Controls.Add(Provider);

            Lbl(cardAi, "模型", 18, 94, 88);
            Model = new InputBox();
            Model.SetBounds(112, 94, 420, 32);
            Model.Inner.TextChanged += delegate { if (Cfg != null) { Cfg.ai.model = Model.Text; Save(); } };
            cardAi.Controls.Add(Model);

            Lbl(cardAi, "API-URL", 18, 134, 88);
            BaseUrl = new InputBox();
            BaseUrl.SetMono(true);
            BaseUrl.SetBounds(112, 134, 420, 32);
            BaseUrl.Inner.TextChanged += delegate { if (Cfg != null) { Cfg.ai.baseURL = BaseUrl.Text; Save(); } };
            cardAi.Controls.Add(BaseUrl);

            Lbl(cardAi, "API-KEY", 18, 174, 88);
            ApiKey = new InputBox();
            ApiKey.SetMono(true);
            ApiKey.IsPassword = true;
            ApiKey.SetBounds(112, 174, 366, 32);
            ApiKey.Inner.TextChanged += delegate { if (Cfg != null) { Cfg.ai.apiKey = ApiKey.Text; Save(); } };
            cardAi.Controls.Add(ApiKey);

            keyBtn = new FlatButton();
            keyBtn.Text = "显示";
            keyBtn.Font = T.FSmall;
            keyBtn.SetBounds(486, 174, 46, 32);
            keyBtn.Click += delegate
            {
                ApiKey.IsPassword = !ApiKey.IsPassword;
                ApiKey.Inner.UseSystemPasswordChar = ApiKey.IsPassword;
                keyBtn.Text = ApiKey.IsPassword ? "显示" : "隐藏";
                ApiKey.Invalidate();
            };
            cardAi.Controls.Add(keyBtn);

            Lbl(cardAi, "昵称", 18, 254, 88);
            Nickname = new InputBox();
            Nickname.SetBounds(112, 254, 420, 32);
            Nickname.Inner.TextChanged += delegate { if (Cfg != null) { Cfg.character.nickname = Nickname.Text; Save(); } };
            cardAi.Controls.Add(Nickname);

            Lbl(cardAi, "提示词", 18, 294, 88);
            PromptBtn = new FlatButton();
            PromptBtn.Text = "编辑提示词";
            PromptBtn.Font = T.FSmall;
            PromptBtn.Kind = BtnKind.Ghost;
            PromptBtn.SetBounds(112, 294, 132, 32);
            PromptBtn.Click += delegate { EditPrompt(); };
            cardAi.Controls.Add(PromptBtn);

            promptInfo = new Label();
            promptInfo.Text = "使用 OVO 默认人格";
            promptInfo.Font = T.FSmall;
            promptInfo.ForeColor = T.Muted;
            promptInfo.BackColor = Color.Transparent;
            promptInfo.SetBounds(252, 294, 280, 32);
            cardAi.Controls.Add(promptInfo);

            Lbl(cardAi, "温度", 18, 214, 88);
            Temp = new Slider();
            Temp.SetBounds(112, 214, 340, 24);
            Temp.Changed += delegate
            {
                tempVal.Text = Temp.Value.ToString("0.0");
                if (Cfg != null) { Cfg.ai.temperature = Temp.Value; Save(); }
            };
            cardAi.Controls.Add(Temp);

            tempVal = new Label();
            tempVal.Text = "0.7";
            tempVal.Font = T.FMono;
            tempVal.ForeColor = T.Green;
            tempVal.AutoSize = false;
            tempVal.TextAlign = ContentAlignment.MiddleRight;
            tempVal.BackColor = Color.Transparent;
            tempVal.SetBounds(456, 216, 40, 20);
            cardAi.Controls.Add(tempVal);
        }

        void BuildCharCard()
        {
            cardChar = new Card();
            cardChar.Title = "角色设置";
            cardChar.Subtitle = "机器人进服时使用的名字与皮肤";
            cardChar.Accent = T.Green;
            cardChar.SetBounds(592, 12, 564, 286);
            Controls.Add(cardChar);

            Lbl(cardChar, "游戏名", 18, 54, 88);
            Username = new InputBox();
            Username.SetBounds(112, 54, 300, 32);
            Username.Inner.MaxLength = 16;
            Username.Inner.TextChanged += delegate { if (Cfg != null) { Cfg.character.username = Username.Text; Save(); } };
            cardChar.Controls.Add(Username);

            Lbl(cardChar, "皮肤路径", 18, 94, 88);
            SkinPath = new InputBox();
            SkinPath.SetMono(true);
            SkinPath.SetBounds(112, 94, 300, 32);
            SkinPath.Inner.TextChanged += delegate
            {
                if (Cfg != null)
                {
                    Cfg.character.skinPath = SkinPath.Text;
                    Preview.SkinPath = SkinPath.Text;
                    Preview.Reload();
                    Save();
                }
            };
            cardChar.Controls.Add(SkinPath);

            browseBtn = new FlatButton();
            browseBtn.Text = "浏览";
            browseBtn.SetBounds(420, 94, 74, 32);
            browseBtn.Click += delegate
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "皮肤图片 (*.png)|*.png|所有文件 (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    SkinPath.Text = dlg.FileName;
                    if (SkinPicked != null) SkinPicked(this, EventArgs.Empty);
                }
            };
            cardChar.Controls.Add(browseBtn);

            Lbl(cardChar, "皮肤模型", 18, 140, 88);
            ModelSeg = new Segmented();
            ModelSeg.Items = new string[] { "Steve", "Alex" };
            ModelSeg.Font = T.FSmall;
            ModelSeg.SetBounds(112, 140, 200, 34);
            ModelSeg.Changed += delegate
            {
                if (Cfg != null)
                {
                    Cfg.character.skinModel = ModelSeg.SelectedIndex == 1 ? "alex" : "steve";
                    Preview.Model = Cfg.character.skinModel;
                    Preview.Reload();
                    Save();
                }
            };
            cardChar.Controls.Add(ModelSeg);

            Preview = new AvatarView();
            Preview.SetBounds(420, 132, 76, 76);
            Preview.BackColor = T.CardSoft;
            cardChar.Controls.Add(Preview);
        }

        void BuildServerCard()
        {
            cardSrv = new Card();
            cardSrv.Title = "服务器";
            cardSrv.Subtitle = "版本在添加 / 编辑服务器时选择，选中服务器的版本就是启动版本";
            cardSrv.Accent = T.Blue;
            cardSrv.SetBounds(24, 310, 1132, 230);
            Controls.Add(cardSrv);

            addBtn = new FlatButton();
            addBtn.Text = "＋ 添加服务器";
            addBtn.Font = T.FSmall;
            addBtn.SetBounds(980, 20, 132, 30);
            addBtn.Click += delegate { if (AddServer != null) AddServer(this, EventArgs.Empty); };
            cardSrv.Controls.Add(addBtn);

            Table = new ServerTable();
            Table.SetBounds(12, 56, 1108, 302);
            Table.DeleteClicked += delegate(object s, EventArgs e)
            {
                RemoveServer(Cfg, ((ServerEntry)s).id);
                Save();
                RefreshServers();
                if (ConfigChanged != null) ConfigChanged(this, EventArgs.Empty);
            };
            Table.EditClicked += delegate(object s, EventArgs e)
            {
                if (EditServer != null) EditServer((ServerEntry)s, EventArgs.Empty);
            };
            Table.SelectClicked += delegate(object s, EventArgs e)
            {
                Cfg.selectedServerId = ((ServerEntry)s).id;
                SyncVersionToServer();
                Save();
                RefreshServers();
                if (ConfigChanged != null) ConfigChanged(this, EventArgs.Empty);
            };
            cardSrv.Controls.Add(Table);
        }

        /// <summary>把启动版本同步成当前选中服务器的版本</summary>
        public void SyncVersionToServer()
        {
            var srv = Cfg.SelectedServer;
            if (srv != null) Cfg.client.version = srv.version;
        }

        /// <summary>
        /// 删除服务器：若删掉的是当前选中项，则自动选中表格最上面那条，并同步版本；
        /// 全部删完则清空选择。
        /// </summary>
        public static void RemoveServer(Config cfg, string serverId)
        {
            cfg.servers.RemoveAll(delegate(ServerEntry x) { return x.id == serverId; });
            if (cfg.selectedServerId == serverId)
            {
                cfg.selectedServerId = cfg.servers.Count > 0 ? cfg.servers[0].id : null;
                var s = cfg.SelectedServer;
                if (s != null) cfg.client.version = s.version;
            }
        }

        void Save()
        {
            Store.Save(Cfg);
            if (ConfigChanged != null) ConfigChanged(this, EventArgs.Empty);
        }

        void OnAiChanged()
        {
            if (Cfg == null) return;
            string[] keys = new string[] { "openai", "deepseek", "moonshot", "custom" };
            string[] urls = new string[] {
                "https://api.openai.com/v1", "https://api.deepseek.com/v1",
                "https://api.moonshot.cn/v1", "" };
            string[] models = new string[] { "gpt-4o-mini", "deepseek-chat", "moonshot-v1-8k", "" };
            int i = Provider.SelectedIndex;
            Cfg.ai.provider = keys[i];
            BaseUrl.Text = urls[i];
            Cfg.ai.baseURL = urls[i];
            if (models[i].Length > 0)
            {
                Model.Text = models[i];
                Cfg.ai.model = models[i];
            }

            // 昵称按提供商给默认值（用户改过的就不动）
            string[] nicknames = new string[] { "龙娘", "鲸鲸", "", "" };
            string cur = Nickname.Text == null ? "" : Nickname.Text.Trim();
            bool isDefault = cur.Length == 0 || cur == "龙娘" || cur == "鲸鲸";
            if (isDefault)
            {
                Nickname.Text = nicknames[i];
                Cfg.character.nickname = nicknames[i];
            }
            Save();
        }

        /// <summary>编辑 AI 人格提示词</summary>
        void EditPrompt()
        {
            if (Cfg == null) return;
            string provider = Cfg.ai.provider == "deepseek" ? "DeepSeek"
                : Cfg.ai.provider == "openai" ? "OpenAI"
                : Cfg.ai.provider == "moonshot" ? "Moonshot" : "自定义";
            string hint = "当前提供商：" + provider + "　留空则使用 OVO 自带人格提示词；填入后会被写入 OVO 的 profile（conversing 字段）";
            var dlg = new PromptDialog(Cfg.ai.systemPrompt, hint);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            Cfg.ai.systemPrompt = dlg.PromptText;
            Save();
            UpdatePromptInfo();
        }

        void UpdatePromptInfo()
        {
            if (promptInfo == null || Cfg == null) return;
            int n = Cfg.ai.systemPrompt == null ? 0 : Cfg.ai.systemPrompt.Trim().Length;
            promptInfo.Text = n == 0 ? "使用 OVO 默认人格" : ("已自定义 · " + n + " 字");
            promptInfo.ForeColor = n == 0 ? T.Muted : T.Accent;
        }

        public void Bind(Config cfg)
        {
            Cfg = cfg;
            string[] keys = new string[] { "openai", "deepseek", "moonshot", "custom" };
            int idx = Array.IndexOf(keys, cfg.ai.provider);
            Provider.SelectedIndex = idx < 0 ? 0 : idx;
            Model.Text = cfg.ai.model;
            BaseUrl.Text = cfg.ai.baseURL;
            ApiKey.Text = cfg.ai.apiKey;
            Temp.Value = cfg.ai.temperature;
            tempVal.Text = cfg.ai.temperature.ToString("0.0");

            Nickname.Text = cfg.character.nickname == null ? "" : cfg.character.nickname;
            UpdatePromptInfo();

            Username.Text = cfg.character.username;
            SkinPath.Text = cfg.character.skinPath;
            ModelSeg.SelectedIndex = cfg.character.skinModel == "alex" ? 1 : 0;
            Preview.SkinPath = cfg.character.skinPath;
            Preview.Model = cfg.character.skinModel;
            Preview.Reload();

            SetVersion(cfg.client.version);
            RefreshServers();
        }

        public void SetVersion(string id)
        {
            if (Cfg != null) Cfg.client.version = id;
            if (Cfg != null) Save();
        }

        public void RefreshServers()
        {
            Table.Items = Cfg.servers;
            Table.SelectedId = Cfg.selectedServerId;
            LayoutPage();
            Table.Invalidate();
        }

        public int VersionCount { get { return VersionRepo.Items.Count; } }
    }

    // ---------------------------------------------------------------- 启动页
    public class LaunchPage : Panel
    {
        public Config Cfg;
        AvatarView avatar;
        Label nameLbl, subLbl, srvLbl;
        InfoRow rowVersion, rowProvider, rowUrl, rowKey;
        public SelectBox ServerSelect;
        public FlatButton LaunchBtn, StopBtn;
        public LogPanel Log;
        public string Tip = "";
        public bool ForceTip = false;
        bool hoverLaunch;
        public event EventHandler ServerChanged;
        public event EventHandler LaunchClicked;
        public event EventHandler StopClicked;
        Card cardProfile, cardInfo, cardLog;

        public LaunchPage()
        {
            BackColor = T.Bg;

            cardProfile = new Card();
            cardProfile.SetBounds(24, 12, 340, 120);
            Controls.Add(cardProfile);

            avatar = new AvatarView();
            avatar.Big = true;
            avatar.SetBounds(16, 14, 92, 92);
            avatar.BackColor = T.CardSoft;
            cardProfile.Controls.Add(avatar);

            nameLbl = new Label();
            nameLbl.Text = "MineBot";
            nameLbl.Font = T.FBig;
            nameLbl.ForeColor = T.Text;
            nameLbl.BackColor = Color.Transparent;
            nameLbl.AutoSize = false;
            nameLbl.SetBounds(124, 26, 200, 26);
            cardProfile.Controls.Add(nameLbl);

            subLbl = new Label();
            subLbl.Text = "未选择服务器";
            subLbl.Font = T.FSmall;
            subLbl.ForeColor = T.Muted;
            subLbl.BackColor = Color.Transparent;
            subLbl.AutoSize = false;
            subLbl.SetBounds(124, 56, 200, 40);
            cardProfile.Controls.Add(subLbl);

            cardInfo = new Card();
            cardInfo.SetBounds(24, 144, 340, 228);
            Controls.Add(cardInfo);

            srvLbl = new Label();
            srvLbl.Text = "服务器";
            srvLbl.Font = T.FSmall;
            srvLbl.ForeColor = T.Muted;
            srvLbl.BackColor = Color.Transparent;
            srvLbl.AutoSize = false;
            srvLbl.SetBounds(8, 6, 84, 32);
            srvLbl.TextAlign = ContentAlignment.MiddleLeft;
            cardInfo.Controls.Add(srvLbl);

            ServerSelect = new SelectBox();
            ServerSelect.Font = T.FSmall;
            ServerSelect.SetBounds(96, 8, 236, 30);
            ServerSelect.BackColor = T.Card;
            ServerSelect.Changed += delegate
            {
                if (Cfg == null) return;
                int i = ServerSelect.SelectedIndex;
                if (i >= 0 && i < Cfg.servers.Count)
                {
                    Cfg.selectedServerId = Cfg.servers[i].id;
                    Cfg.client.version = Cfg.servers[i].version;   // 自动匹配版本
                    Store.Save(Cfg);
                    Bind(Cfg);
                    if (ServerChanged != null) ServerChanged(this, EventArgs.Empty);
                }
            };
            cardInfo.Controls.Add(ServerSelect);

            rowVersion = new InfoRow();
            rowVersion.Key = "已选择版本";
            rowVersion.SetBounds(8, 46, 324, 42);
            cardInfo.Controls.Add(rowVersion);

            rowProvider = new InfoRow();
            rowProvider.Key = "AI 提供商";
            rowProvider.SetBounds(8, 88, 324, 42);
            cardInfo.Controls.Add(rowProvider);

            rowUrl = new InfoRow();
            rowUrl.Key = "API-URL";
            rowUrl.SetBounds(8, 130, 324, 42);
            cardInfo.Controls.Add(rowUrl);

            rowKey = new InfoRow();
            rowKey.Key = "API-KEY";
            rowKey.SetBounds(8, 172, 324, 42);
            cardInfo.Controls.Add(rowKey);

            LaunchBtn = new FlatButton();
            LaunchBtn.Text = "启 动";
            LaunchBtn.Kind = BtnKind.Primary;
            LaunchBtn.Font = T.FLaunch;
            LaunchBtn.SetBounds(24, 384, 340, 54);
            LaunchBtn.Click += delegate { if (LaunchClicked != null) LaunchClicked(this, EventArgs.Empty); };
            LaunchBtn.MouseEnter += delegate { hoverLaunch = true; Invalidate(); };
            LaunchBtn.MouseLeave += delegate { hoverLaunch = false; Invalidate(); };
            Controls.Add(LaunchBtn);

            StopBtn = new FlatButton();
            StopBtn.Text = "停止";
            StopBtn.Kind = BtnKind.Danger;
            StopBtn.SetBounds(24, 448, 340, 38);
            StopBtn.Visible = false;
            StopBtn.Click += delegate { if (StopClicked != null) StopClicked(this, EventArgs.Empty); };
            Controls.Add(StopBtn);

            cardLog = new Card();
            cardLog.SetBounds(380, 12, 776, 612);
            Controls.Add(cardLog);

            var head = new Label();
            head.Text = "● 运行日志";
            head.Font = T.FCardTitle;
            head.ForeColor = T.Text;
            head.BackColor = Color.Transparent;
            head.AutoSize = false;
            head.SetBounds(18, 18, 200, 22);
            cardLog.Controls.Add(head);

            Log = new LogPanel();
            Log.SetBounds(14, 52, 748, 544);
            cardLog.Controls.Add(Log);
        }

        /// <summary>自适应布局：左栏固定宽，右边日志区随窗口伸缩</summary>
        public void LayoutPage()
        {
            if (cardProfile == null || cardLog == null || Log == null) return;
            const int leftW = 340, m = 24, gap = 16;

            cardProfile.SetBounds(m, 12, leftW, 120);
            cardInfo.SetBounds(m, 144, leftW, 228);

            // 启动 / 停止按钮贴在左栏底部
            int bottom = Height - 24;
            if (bottom < 560) bottom = 560;
            StopBtn.SetBounds(m, bottom - 92, leftW, 38);
            LaunchBtn.SetBounds(m, bottom - 54, leftW, 54);

            int innerW = leftW - 16;
            ServerSelect.SetBounds(96, 8, leftW - 96 - 16, 30);
            rowVersion.SetBounds(8, 46, innerW, 42);
            rowProvider.SetBounds(8, 88, innerW, 42);
            rowUrl.SetBounds(8, 130, innerW, 42);
            rowKey.SetBounds(8, 172, innerW, 42);

            int logX = m + leftW + gap;
            int logW = Width - logX - m;
            int logH = Height - 24;
            if (logW < 200) logW = 200;
            if (logH < 160) logH = 160;

            cardLog.SetBounds(logX, 12, logW, logH);
            Log.SetBounds(14, 52, logW - 28, logH - 66);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutPage();
        }

        public void Bind(Config cfg)
        {
            Cfg = cfg;
            nameLbl.Text = cfg.character.username;
            var srv = cfg.SelectedServer;
            subLbl.Text = srv == null
                ? "未选择服务器"
                : (srv.host + ":" + srv.port + (string.IsNullOrEmpty(srv.note) ? "" : " · " + srv.note));
            avatar.SkinPath = cfg.character.skinPath;
            avatar.Model = cfg.character.skinModel;
            avatar.Reload();

            // 服务器下拉：列出全部服务器，选中项与 selectedServerId 对齐
            string[] items = new string[cfg.servers.Count];
            int idx = -1;
            for (int i = 0; i < cfg.servers.Count; i++)
            {
                var s = cfg.servers[i];
                items[i] = s.host + ":" + s.port + (string.IsNullOrEmpty(s.note) ? "" : " · " + s.note);
                if (s.id == cfg.selectedServerId) idx = i;
            }
            if (items.Length == 0)
            {
                items = new string[] { "（暂无服务器）" };
                idx = 0;
            }
            ServerSelect.Items = items;
            ServerSelect.SelectedIndex = idx < 0 ? 0 : idx;
            ServerSelect.Enabled = cfg.servers.Count > 0;
            ServerSelect.Invalidate();

            rowVersion.Val = cfg.client.version;
            rowProvider.Val = cfg.ai.Label;
            rowUrl.Val = string.IsNullOrEmpty(cfg.ai.baseURL) ? "—" : cfg.ai.baseURL;
            rowKey.Val = MaskKey(cfg.ai.apiKey);
            rowVersion.Invalidate();
            rowProvider.Invalidate();
            rowUrl.Invalidate();
            rowKey.Invalidate();

            string blocker = Blocker();
            Tip = blocker;
            LaunchBtn.Enabled = blocker.Length == 0;
            Invalidate();
        }

        public string Blocker()
        {
            if (Cfg == null) return "";
            var srv = Cfg.SelectedServer;
            if (srv == null) return "还没有选择服务器：请先在配置页添加服务器，或在左侧下拉里选一个";
            if (srv.version != Cfg.client.version)
                return string.Format("版本不匹配：服务器是 {0}，启动器是 {1}，请把两者改成同一个版本", srv.version, Cfg.client.version);
            return "";
        }

        public void SetRunning(bool running)
        {
            StopBtn.Visible = running;
        }

        static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "未填写";
            if (key.Length <= 10) return new string('•', key.Length);
            return key.Substring(0, 6) + new string('•', Math.Min(18, key.Length - 10)) + key.Substring(key.Length - 4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Tip.Length == 0) return;
            if (!ForceTip && !hoverLaunch) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 提示气泡贴在启动按钮上方
            int tipY = LaunchBtn.Top - 64;
            if (tipY < cardInfo.Bottom + 8) tipY = cardInfo.Bottom + 8;
            var r = new Rectangle(24, tipY, 340, 56);
            using (var br = new SolidBrush(T.WarnSoft))
                g.FillPath(br, T.Round(r, 10));
            T.Stroke(g, r, 10, Color.FromArgb(238, 214, 170));
            using (var b = new SolidBrush(T.Warn))
            {
                var sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;
                g.DrawString(Tip, T.FSmall, b, new RectangleF(r.X + 12, r.Y + 6, r.Width - 24, r.Height - 12), sf);
                sf.Dispose();
            }
        }
    }
}
