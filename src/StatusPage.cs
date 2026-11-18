using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace McBot
{
    /// <summary>背包网格（文字瓦片，无贴图）</summary>
    public class InventoryView : Control
    {
        public List<string> Names = new List<string>();
        public List<int> Counts = new List<int>();
        public string EmptyText = "背包是空的（先启动机器人）";

        public InventoryView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Card;
        }

        public void SetItems(List<string> names, List<int> counts)
        {
            Names = names; Counts = counts;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (Names.Count == 0)
            {
                T.TextAt(g, EmptyText, T.FSmall, T.Muted, 4, 6);
                return;
            }

            int tileW = 118, tileH = 34, gap = 8;
            int perRow = Math.Max(1, (Width - gap) / (tileW + gap));
            for (int i = 0; i < Names.Count; i++)
            {
                int col = i % perRow;
                int row = i / perRow;
                int x = gap + col * (tileW + gap);
                int y = gap + row * (tileH + gap);
                if (y + tileH > Height) break;

                var r = new Rectangle(x, y, tileW, tileH);
                T.Fill(g, r, 7, T.CardSoft);
                T.Stroke(g, r, 7, T.Line);
                T.TextAt(g, Names[i], T.FMonoSmall, T.Dim, x + 8, y, tileW - 46, tileH, StringAlignment.Near);
                T.TextAt(g, "×" + Counts[i], T.FMonoSmall, T.Accent, x + tileW - 44, y, 38, tileH, StringAlignment.Far);
            }
        }
    }

    /// <summary>状态页：连接状态 / 对话 / 背包 / 控制（引擎固定为内嵌 OVO）</summary>
    public class StatusPage : Panel
    {
        public Config Cfg;

        Card cardProfile, cardControl, cardChat, cardInv;
        AvatarView avatar;
        Label nameLbl, phaseLbl, serverLbl, healthLbl, posLbl, engineHint;
        public FlatButton RestartBtn, DisconnectBtn;
        public Segmented ReplySeg, ScopeSeg;
        public InputBox WhitelistInput;
        public LogPanel ChatLog;
        public InventoryView Inv;

        public event EventHandler ReplyModeChanged;
        public event EventHandler ScopeChanged;
        public event EventHandler RestartBot;
        public event EventHandler DisconnectBot;

        bool inGame = false;

        public StatusPage()
        {
            BackColor = T.Bg;
            BuildProfile();
            BuildControl();
            BuildChat();
            BuildInventory();
        }

        void BuildProfile()
        {
            cardProfile = new Card();
            cardProfile.Title = "机器人状态";
            cardProfile.Accent = T.Accent;
            cardProfile.SetBounds(24, 12, 340, 190);
            Controls.Add(cardProfile);

            avatar = new AvatarView();
            avatar.SetBounds(16, 48, 72, 72);
            avatar.BackColor = T.CardSoft;
            cardProfile.Controls.Add(avatar);

            nameLbl = new Label();
            nameLbl.Text = "未启动";
            nameLbl.Font = T.FBig;
            nameLbl.ForeColor = T.Text;
            nameLbl.BackColor = Color.Transparent;
            nameLbl.SetBounds(102, 50, 210, 26);
            cardProfile.Controls.Add(nameLbl);

            phaseLbl = new Label();
            phaseLbl.Text = "离线";
            phaseLbl.Font = T.FSmall;
            phaseLbl.ForeColor = T.Muted;
            phaseLbl.BackColor = Color.Transparent;
            phaseLbl.SetBounds(102, 78, 210, 20);
            cardProfile.Controls.Add(phaseLbl);

            serverLbl = new Label();
            serverLbl.Text = "—";
            serverLbl.Font = T.FMonoSmall;
            serverLbl.ForeColor = T.Dim;
            serverLbl.BackColor = Color.Transparent;
            serverLbl.SetBounds(102, 100, 220, 18);
            cardProfile.Controls.Add(serverLbl);

            healthLbl = new Label();
            healthLbl.Text = "血量 — · 饥饿 —";
            healthLbl.Font = T.FSmall;
            healthLbl.ForeColor = T.Dim;
            healthLbl.BackColor = Color.Transparent;
            healthLbl.SetBounds(16, 130, 300, 18);
            cardProfile.Controls.Add(healthLbl);

            posLbl = new Label();
            posLbl.Text = "坐标 —";
            posLbl.Font = T.FMonoSmall;
            posLbl.ForeColor = T.Muted;
            posLbl.BackColor = Color.Transparent;
            posLbl.SetBounds(16, 152, 300, 18);
            cardProfile.Controls.Add(posLbl);
        }

        void BuildControl()
        {
            cardControl = new Card();
            cardControl.Title = "控制";
            cardControl.Accent = T.Blue;
            cardControl.SetBounds(24, 214, 340, 306);
            Controls.Add(cardControl);

            engineHint = new Label();
            engineHint.Text = "回复方式（切换即时生效）";
            engineHint.Font = T.FSmall;
            engineHint.ForeColor = T.Dim;
            engineHint.BackColor = Color.Transparent;
            engineHint.SetBounds(16, 46, 305, 18);
            cardControl.Controls.Add(engineHint);

            ReplySeg = new Segmented();
            ReplySeg.Items = new string[] { "私信回复", "公屏@回复" };
            ReplySeg.Font = T.FSmall;
            ReplySeg.SetBounds(16, 66, 230, 32);
            ReplySeg.Changed += delegate { if (ReplyModeChanged != null) ReplyModeChanged(this, EventArgs.Empty); };
            cardControl.Controls.Add(ReplySeg);

            var l2 = new Label();
            l2.Text = "回应范围";
            l2.Font = T.FSmall;
            l2.ForeColor = T.Dim;
            l2.BackColor = Color.Transparent;
            l2.SetBounds(16, 108, 305, 18);
            cardControl.Controls.Add(l2);

            ScopeSeg = new Segmented();
            ScopeSeg.Items = new string[] { "所有人", "仅白名单" };
            ScopeSeg.Font = T.FSmall;
            ScopeSeg.SetBounds(16, 128, 230, 32);
            ScopeSeg.Changed += delegate { if (ScopeChanged != null) ScopeChanged(this, EventArgs.Empty); };
            cardControl.Controls.Add(ScopeSeg);

            WhitelistInput = new InputBox();
            WhitelistInput.SetBounds(16, 168, 308, 32);
            WhitelistInput.Inner.TextChanged += delegate { if (ScopeChanged != null) ScopeChanged(this, EventArgs.Empty); };
            cardControl.Controls.Add(WhitelistInput);

            RestartBtn = new FlatButton();
            RestartBtn.Text = "重启机器人";
            RestartBtn.Kind = BtnKind.Ghost;
            RestartBtn.SetBounds(16, 210, 308, 38);
            RestartBtn.Click += delegate { if (RestartBot != null) RestartBot(this, EventArgs.Empty); };
            cardControl.Controls.Add(RestartBtn);

            DisconnectBtn = new FlatButton();
            DisconnectBtn.Text = "断开连接";
            DisconnectBtn.Kind = BtnKind.Danger;
            DisconnectBtn.SetBounds(16, 256, 308, 38);
            DisconnectBtn.Click += delegate { if (DisconnectBot != null) DisconnectBot(this, EventArgs.Empty); };
            cardControl.Controls.Add(DisconnectBtn);
        }

        void BuildChat()
        {
            cardChat = new Card();
            cardChat.Title = "对话";
            cardChat.Subtitle = "游戏里私信她（/msg 机器人名 内容），往来会显示在这里";
            cardChat.Accent = T.AccentDeep;
            cardChat.SetBounds(380, 12, 776, 330);
            Controls.Add(cardChat);

            ChatLog = new LogPanel();
            ChatLog.SetBounds(14, 54, 748, 262);
            cardChat.Controls.Add(ChatLog);
        }

        void BuildInventory()
        {
            cardInv = new Card();
            cardInv.Title = "背包";
            cardInv.Subtitle = "启动后实时显示她身上的物品";
            cardInv.Accent = T.Violet;
            cardInv.SetBounds(380, 354, 776, 260);
            Controls.Add(cardInv);

            Inv = new InventoryView();
            Inv.SetBounds(14, 52, 748, 196);
            cardInv.Controls.Add(Inv);
        }

        public void LayoutPage()
        {
            if (cardProfile == null || cardChat == null || cardInv == null) return;
            const int m = 24, leftW = 340, gap = 16;
            int pageH = Height;

            cardProfile.SetBounds(m, 12, leftW, 190);
            cardControl.SetBounds(m, 214, leftW, 306);

            int rightX = m + leftW + gap;
            int rightW = Width - rightX - m;
            if (rightW < 320) rightW = 320;

            int chatH = Math.Min(320, Math.Max(240, (pageH - 40) / 2 + 30));
            cardChat.SetBounds(rightX, 12, rightW, chatH);
            ChatLog.SetBounds(14, 54, rightW - 28, chatH - 68);

            int invY = 12 + chatH + gap;
            int invH = Math.Max(140, pageH - invY - 16);
            cardInv.SetBounds(rightX, invY, rightW, invH);
            Inv.SetBounds(14, 52, rightW - 28, invH - 64);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutPage();
        }

        public void Refresh(Config cfg)
        {
            Cfg = cfg;
            avatar.SkinPath = cfg.character.skinPath;
            avatar.Model = cfg.character.skinModel;
            avatar.Reload();

            if (!inGame) nameLbl.Text = cfg.character.username + "（未启动）";
            else nameLbl.Text = cfg.character.username;

            var srv = cfg.SelectedServer;
            serverLbl.Text = srv == null ? "未选择服务器" : (srv.host + ":" + srv.port + " · " + srv.version);
            ReplySeg.SelectedIndex = (cfg.bot != null && cfg.bot.replyMode == "public") ? 1 : 0;
            ReplySeg.Invalidate();
            ScopeSeg.SelectedIndex = (cfg.bot != null && cfg.bot.whitelistEnabled) ? 1 : 0;
            ScopeSeg.Invalidate();
            if (!WhitelistInput.Inner.Focused)
                WhitelistInput.Text = cfg.bot != null && cfg.bot.whitelist != null ? cfg.bot.whitelist : "";
            LayoutPage();
        }

        public bool WhitelistOnly
        {
            get { return ScopeSeg.SelectedIndex == 1; }
        }

        public string WhitelistText
        {
            get { return WhitelistInput.Text == null ? "" : WhitelistInput.Text.Trim(); }
        }

        public bool PublicReply
        {
            get { return ReplySeg.SelectedIndex == 1; }
        }

        public void SetRunning(bool running)
        {
            RestartBtn.Enabled = running;
            DisconnectBtn.Enabled = running;
            if (!running)
            {
                inGame = false;
                phaseLbl.Text = "离线";
                phaseLbl.ForeColor = T.Muted;
                healthLbl.Text = "血量 — · 饥饿 —";
                posLbl.Text = "坐标 —";
                Inv.SetItems(new List<string>(), new List<int>());
                if (Cfg != null) nameLbl.Text = Cfg.character.username + "（未启动）";
            }
        }

        public void AppendChat(string from, string name, string text, string channel)
        {
            string level = "info";
            string who = name;
            if (from == "user") { level = "chat"; who = name + " →"; }
            else if (from == "bot")
            {
                level = "ai";
                string tag = "";
                if (channel == "self") tag = "（自言自语）";
                else if (channel == "public") tag = "（公屏）";
                else if (channel == "whisper") tag = "（私聊）";
                who = "鲸鲸" + tag + " →";
            }
            else { level = "muted"; who = ""; }
            ChatLog.Append(level, (who.Length > 0 ? who + " " : "") + text);
        }

        public void AppendSystem(string text)
        {
            ChatLog.Append("muted", text);
        }

        /// <summary>处理 OVO 推来的结构化数据</summary>
        public void HandleData(string kind, string json)
        {
            try
            {
                var ser = new JavaScriptSerializer();
                var d = (Dictionary<string, object>)ser.DeserializeObject(json);
                if (d == null) return;

                if (kind == "inventory")
                {
                    var names = new List<string>();
                    var counts = new List<int>();
                    var arr = d.ContainsKey("items") ? d["items"] as object[] : null;
                    if (arr != null)
                    {
                        foreach (var o in arr)
                        {
                            var it = (Dictionary<string, object>)o;
                            if (it == null || !it.ContainsKey("name")) continue;
                            names.Add(Convert.ToString(it["name"]));
                            counts.Add(it.ContainsKey("count") ? Convert.ToInt32(it["count"]) : 1);
                        }
                    }
                    Inv.SetItems(names, counts);
                }
                else if (kind == "state")
                {
                    bool nowIn = d.ContainsKey("in_game") && Convert.ToBoolean(d["in_game"]);
                    inGame = nowIn;
                    string phase = d.ContainsKey("phase") ? Convert.ToString(d["phase"]) : "offline";
                    if (nowIn) { phaseLbl.Text = "已连接"; phaseLbl.ForeColor = T.Ok; }
                    else if (phase == "connecting") { phaseLbl.Text = "连接中…"; phaseLbl.ForeColor = T.Warn; }
                    else { phaseLbl.Text = "离线"; phaseLbl.ForeColor = T.Muted; }

                    if (Cfg != null) nameLbl.Text = nowIn ? Cfg.character.username : Cfg.character.username + "（未启动）";

                    string gm = d.ContainsKey("gameMode") && d["gameMode"] != null ? Convert.ToString(d["gameMode"]) : "";
                    string gmText = "";
                    if (gm == "survival") gmText = " · 生存";
                    else if (gm == "creative") gmText = " · 创造";
                    else if (gm == "adventure") gmText = " · 冒险";
                    else if (gm == "spectator") gmText = " · 旁观";

                    if (d.ContainsKey("health") && d["health"] != null)
                    {
                        string h = Convert.ToString(d["health"]);
                        string f = d.ContainsKey("food") && d["food"] != null ? Convert.ToString(d["food"]) : "—";
                        healthLbl.Text = "血量 " + h + " · 饥饿 " + f + gmText;
                        int hv = Convert.ToInt32(d["health"]);
                        healthLbl.ForeColor = hv <= 6 ? T.Danger : (gm == "adventure" ? T.Warn : T.Dim);
                    }

                    if (d.ContainsKey("pos") && d["pos"] != null)
                    {
                        var p = (Dictionary<string, object>)d["pos"];
                        posLbl.Text = "坐标 " + Convert.ToString(p["x"]) + ", " + Convert.ToString(p["y"]) + ", " + Convert.ToString(p["z"]);
                    }
                }
                else if (kind == "chat")
                {
                    string from = d.ContainsKey("from") ? Convert.ToString(d["from"]) : "system";
                    string name = d.ContainsKey("name") ? Convert.ToString(d["name"]) : "";
                    string text = d.ContainsKey("text") ? Convert.ToString(d["text"]) : "";
                    string ch = d.ContainsKey("channel") ? Convert.ToString(d["channel"]) : "";
                    AppendChat(from, name, text, ch);
                }
            }
            catch { }
        }
    }
}
