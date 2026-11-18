using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace McBot
{
    // ---------------------------------------------------------------- 卡片
    public class Card : Panel
    {
        public string Title = "";
        public string Subtitle = "";
        public Color Accent = T.Green;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            T.Fill(g, r, 12, T.Card);
            T.Stroke(g, r, 12, T.Line);

            if (Title.Length > 0)
            {
                T.Fill(g, new Rectangle(18, 22, 7, 7), 3, Accent);
                T.TextAt(g, Title, T.FCardTitle, T.Text, 33, 17);
                if (Subtitle.Length > 0)
                    T.TextAt(g, Subtitle, T.FSmall, T.Muted, 33, 36);
            }
            base.OnPaint(e);
        }
    }

    // ---------------------------------------------------------------- 按钮
    public enum BtnKind { Primary, Ghost, Danger }

    public class FlatButton : Control
    {
        public BtnKind Kind = BtnKind.Ghost;
        bool hover, down;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            if (!Enabled)
            {
                T.Fill(g, r, 8, T.CardSoft);
                T.Stroke(g, r, 8, T.Line);
                T.TextAt(g, Text, T.FBase, T.Muted, 0, 0, Width, Height, StringAlignment.Center);
                return;
            }

            if (Kind == BtnKind.Primary)
            {
                Color a = hover ? T.AccentLight : T.Mix(T.Accent, T.AccentDeep, 0.25);
                Color b = down ? T.AccentDeep : T.Accent;
                T.FillGrad(g, r, 8, a, b);
                T.TextAt(g, Text, Font ?? T.FBase, T.OnAccent, 0, 0, Width, Height, StringAlignment.Center);
            }
            else
            {
                Color bg = hover ? T.Hover : T.Card;
                if (Kind == BtnKind.Danger)
                {
                    bg = hover ? Color.FromArgb(250, 226, 231) : T.DangerSoft;
                    T.Fill(g, r, 8, bg);
                    T.Stroke(g, r, 8, Color.FromArgb(240, 205, 212));
                    T.TextAt(g, Text, Font ?? T.FBase, T.Danger, 0, 0, Width, Height, StringAlignment.Center);
                    return;
                }
                T.Fill(g, r, 8, bg);
                T.Stroke(g, r, 8, T.LineStrong);
                T.TextAt(g, Text, Font ?? T.FBase, hover ? T.Text : T.Dim, 0, 0, Width, Height, StringAlignment.Center);
            }
        }
    }

    // ---------------------------------------------------------------- 分段控件
    public class Segmented : Control
    {
        public string[] Items = new string[0];
        public int SelectedIndex = 0;
        public int ItemWidth = 0;             // 0 = 自动均分
        public event EventHandler Changed;
        int hoverIndex = -1;

        public Segmented()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Cursor = Cursors.Hand;
        }

        int IndexAt(int x)
        {
            if (Items.Length == 0) return -1;
            int w = ItemWidth > 0 ? ItemWidth : (Width - 6) / Items.Length;
            int i = (x - 3) / w;
            if (i < 0 || i >= Items.Length) return -1;
            return i;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int i = IndexAt(e.X);
            if (i != hoverIndex) { hoverIndex = i; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { hoverIndex = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int i = IndexAt(e.X);
            if (i >= 0 && i != SelectedIndex)
            {
                SelectedIndex = i;
                Invalidate();
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            T.Fill(g, r, 8, T.Input);
            T.Stroke(g, r, 8, T.LineStrong);

            int w = ItemWidth > 0 ? ItemWidth : (Width - 6) / Math.Max(1, Items.Length);
            for (int i = 0; i < Items.Length; i++)
            {
                var ir = new Rectangle(3 + i * w, 3, w, Height - 7);
                if (i == SelectedIndex)
                {
                    T.FillGrad(g, ir, 6, T.AccentLight, T.Accent);
                    T.TextAt(g, Items[i], Font ?? T.FBase, T.OnAccent, ir.X, ir.Y - 1, ir.Width, ir.Height, StringAlignment.Center);
                }
                else
                {
                    if (i == hoverIndex) T.Fill(g, ir, 6, T.Hover);
                    T.TextAt(g, Items[i], Font ?? T.FBase, i == hoverIndex ? T.Text : T.Dim, ir.X, ir.Y - 1, ir.Width, ir.Height, StringAlignment.Center);
                }
            }
        }
    }

    // ---------------------------------------------------------------- 输入框
    public class InputBox : Panel
    {
        TextBox box = new TextBox();
        public bool IsPassword = false;

        public InputBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = T.Input;
            box.ForeColor = T.Text;
            box.Font = T.FBase;
            Controls.Add(box);
            box.TextChanged += delegate { Invalidate(); };
            box.GotFocus += delegate { Invalidate(); };
            box.LostFocus += delegate { Invalidate(); };
            Height = 32;
        }

        public TextBox Inner { get { return box; } }

        public override string Text
        {
            get { return box.Text; }
            set { box.Text = value; }
        }

        public bool ReadOnlyField
        {
            get { return box.ReadOnly; }
            set { box.ReadOnly = value; }
        }

        public void SetMono(bool mono)
        {
            box.Font = mono ? T.FMono : T.FBase;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int h = box.PreferredHeight;
            box.SetBounds(12, (Height - h) / 2, Width - 24, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            T.Fill(g, r, 8, T.Input);
            T.Stroke(g, r, 8, box.Focused ? T.Accent : T.LineStrong);
            if (IsPassword) box.UseSystemPasswordChar = true;
        }
    }

    // ---------------------------------------------------------------- 下拉框
    public class SelectBox : Control
    {
        public string[] Items = new string[0];
        public int SelectedIndex = 0;
        public event EventHandler Changed;

        public SelectBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Cursor = Cursors.Hand;
        }

        public string SelectedItem
        {
            get
            {
                if (SelectedIndex < 0 || SelectedIndex >= Items.Length) return "";
                return Items[SelectedIndex];
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.Renderer = new DarkMenuRenderer();
            menu.Font = T.FBase;
            for (int i = 0; i < Items.Length; i++)
            {
                int idx = i;
                var it = new ToolStripMenuItem(Items[i]);
                it.BackColor = T.Card;
                it.ForeColor = T.Text;
                it.Click += delegate
                {
                    SelectedIndex = idx;
                    Invalidate();
                    if (Changed != null) Changed(this, EventArgs.Empty);
                };
                menu.Items.Add(it);
            }
            menu.Show(this, new Point(0, Height + 2));
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            T.Fill(g, r, 8, T.Input);
            T.Stroke(g, r, 8, T.LineStrong);
            T.TextAt(g, SelectedItem, Font ?? T.FBase, T.Text, 12, 0, Width - 40, Height, StringAlignment.Near);

            // 下三角
            using (var p = new Pen(T.Muted, 1.6f))
            {
                int cx = Width - 20, cy = Height / 2;
                g.DrawLines(p, new Point[] {
                    new Point(cx - 4, cy - 2), new Point(cx, cy + 3), new Point(cx + 4, cy - 2)
                });
            }
        }
    }

    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColors()) { }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var b = new SolidBrush(T.Card)) e.Graphics.FillRectangle(b, e.AffectedBounds);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
            using (var b = new SolidBrush(e.Item.Selected ? T.AccentSoft : T.Card))
                e.Graphics.FillRectangle(b, r);
        }
    }

    public class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return T.Card; } }
        public override Color MenuItemSelected { get { return T.AccentSoft; } }
        public override Color MenuBorder { get { return T.LineStrong; } }
        public override Color MenuItemBorder { get { return T.LineStrong; } }
    }

    // ---------------------------------------------------------------- 滑块
    public class Slider : Control
    {
        double min = 0, max = 1.5, val = 0.7;
        bool drag;

        public Slider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Cursor = Cursors.Hand;
            Height = 22;
        }

        public double Value
        {
            get { return val; }
            set { val = value; Invalidate(); }
        }

        public event EventHandler Changed;

        void SetFromX(int x)
        {
            int pad = 8;
            double t = (double)(x - pad) / Math.Max(1, Width - pad * 2);
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            val = min + (max - min) * t;
            val = Math.Round(val, 1);
            Invalidate();
            if (Changed != null) Changed(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e) { drag = true; SetFromX(e.X); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { drag = false; base.OnMouseUp(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (drag) SetFromX(e.X); base.OnMouseMove(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int pad = 8;
            int cy = Height / 2;
            var track = new Rectangle(pad, cy - 2, Width - pad * 2, 4);
            T.Fill(g, track, 2, T.Line);
            double t = (val - min) / (max - min);
            int tw = (int)(track.Width * t);
            if (tw > 2) T.Fill(g, new Rectangle(track.X, track.Y, tw, 4), 2, T.Accent);
            int cx = track.X + tw;
            using (var b = new SolidBrush(Color.White))
                g.FillEllipse(b, cx - 6, cy - 6, 12, 12);
            using (var p = new Pen(T.Accent, 2f))
                g.DrawEllipse(p, cx - 6, cy - 6, 12, 12);
        }
    }

    // ---------------------------------------------------------------- 皮肤头像
    public class AvatarView : Control
    {
        public string SkinPath = "";
        public string Model = "steve";
        public bool Big = false;
        Bitmap skin;

        public AvatarView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
        }

        public void Reload()
        {
            if (skin != null) { skin.Dispose(); skin = null; }
            string p = SkinPath;
            if (!string.IsNullOrEmpty(p) && System.IO.File.Exists(p))
            {
                try { skin = new Bitmap(p); } catch { skin = null; }
                Invalidate();
                return;
            }
            // 回退到内置皮肤
            byte[] bytes = Runtime.SkinBytes(Model);
            if (bytes != null)
            {
                try
                {
                    var ms = new System.IO.MemoryStream(bytes);
                    skin = new Bitmap(ms);
                }
                catch { skin = null; }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (skin == null) Reload();
            if (skin == null) return;

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (!Big)
            {
                float u = Math.Min(Width, Height) / 9.4f;
                float ox = (Width - u * 8) / 2f;
                float oy = (Height - u * 8) / 2f;
                Draw(g, 8, 8, 8, 8, ox, oy, u);
                Draw(g, 40, 8, 8, 8, ox, oy, u);
            }
            else
            {
                float u = Math.Min((Width - 14f) / 16f, (Height - 14f) / 20f);
                float totalW = u * 16f;
                float ox = (Width - totalW) / 2f + u * 4f;   // 身体左边缘
                float oy = (Height - u * 20f) / 2f;          // 头顶
                Draw(g, 4, 20, 4, 12, ox, oy + u * 8, u);            // 腿
                Draw(g, 8, 20, 4, 12, ox + u * 4, oy + u * 8, u);
                Draw(g, 20, 20, 8, 12, ox, oy + u * 8, u);           // 身体
                Draw(g, 44, 20, 4, 12, ox - u * 4, oy + u * 8, u);   // 手臂
                Draw(g, 44, 20, 4, 12, ox + u * 8, oy + u * 8, u);
                Draw(g, 8, 8, 8, 8, ox, oy, u);                       // 头
                Draw(g, 40, 8, 8, 8, ox, oy, u);
            }
        }

        void Draw(Graphics g, int sx, int sy, int sw, int sh, float dx, float dy, float u)
        {
            g.DrawImage(skin,
                new RectangleF(dx, dy, sw * u, sh * u),
                new RectangleF(sx, sy, sw, sh),
                GraphicsUnit.Pixel);
        }
    }

    // ---------------------------------------------------------------- 服务器表格
    public class ServerTable : Control
    {
        public List<ServerEntry> Items = new List<ServerEntry>();
        public string SelectedId = null;
        public event EventHandler DeleteClicked;
        public event EventHandler EditClicked;
        public event EventHandler SelectClicked;
        public ServerEntry HoverEntry = null;
        string hoverZone = null;
        int hoverRow = -1;

        const int RowH = 38, HeadH = 26;

        public ServerTable()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
        }

        int[] ColX()
        {
            int w = Width;
            return new int[] { 4, (int)(w * 0.26), (int)(w * 0.36), (int)(w * 0.50), (int)(w * 0.72) };
        }

        int RowAt(int y)
        {
            if (y < HeadH) return -1;
            int i = (y - HeadH) / RowH + scrollRow;
            if (i < 0 || i >= Items.Count) return -1;
            return i;
        }

        int scrollRow = 0;

        int VisibleRows
        {
            get { return Math.Max(1, (Height - HeadH) / RowH); }
        }

        void ClampScroll()
        {
            int max = Math.Max(0, Items.Count - VisibleRows);
            if (scrollRow > max) scrollRow = max;
            if (scrollRow < 0) scrollRow = 0;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            scrollRow -= e.Delta / 120;
            ClampScroll();
            Invalidate();
            base.OnMouseWheel(e);
        }

        string ZoneAt(int x, int y)
        {
            int row = RowAt(y);
            if (row < 0) return null;
            int[] cx = ColX();
            if (x >= cx[4])
            {
                if (x < cx[4] + 52) return "del";
                if (x < cx[4] + 104) return "edit";
                return "sel";
            }
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int row = RowAt(e.Y);
            string zone = ZoneAt(e.X, e.Y);
            Cursor = zone != null ? Cursors.Hand : Cursors.Default;
            if (row != hoverRow || zone != hoverZone)
            {
                hoverRow = row;
                hoverZone = zone;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverRow = -1; hoverZone = null;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int row = RowAt(e.Y);
            string zone = ZoneAt(e.X, e.Y);
            if (row >= 0 && zone != null)
            {
                var handler = zone == "del" ? DeleteClicked : (zone == "edit" ? EditClicked : SelectClicked);
                if (handler != null) handler(Items[row], EventArgs.Empty);
            }
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int[] cx = ColX();

            string[] heads = { "地址", "端口", "版本", "备注", "操作" };
            for (int i = 0; i < heads.Length; i++)
                T.TextAt(g, heads[i], T.FSmall, T.Muted, cx[i] + 10, 4, 120, HeadH - 6, StringAlignment.Near);
            using (var p = new Pen(T.Line, 1f))
                g.DrawLine(p, 4, HeadH - 2, Width - 4, HeadH - 2);

            ClampScroll();
            int last = Math.Min(Items.Count, scrollRow + VisibleRows);
            for (int i = scrollRow; i < last; i++)
            {
                var it = Items[i];
                var rr = new Rectangle(4, HeadH + (i - scrollRow) * RowH, Width - 8, RowH);
                bool sel = it.id == SelectedId;
                if (sel) T.Fill(g, rr, 8, T.RowSelected);
                else if (i == hoverRow) T.Fill(g, rr, 8, T.Hover);

                int ty = rr.Y + 10;
                T.TextAt(g, it.host, T.FMono, T.Text, cx[0] + 10, ty);
                T.TextAt(g, it.port.ToString(), T.FMono, T.Dim, cx[1] + 10, ty);
                T.TextAt(g, it.version, T.FMono, T.Dim, cx[2] + 10, ty);
                T.TextAt(g, string.IsNullOrEmpty(it.note) ? "—" : it.note, T.FSmall, T.Muted, cx[3] + 10, ty + 1);

                // 操作区
                int ax = cx[4] + 10;
                T.TextAt(g, "删除", T.FSmall, hoverRow == i && hoverZone == "del" ? T.Red : T.Dim, ax, ty + 1);
                T.TextAt(g, "编辑", T.FSmall, hoverRow == i && hoverZone == "edit" ? T.Text : T.Dim, ax + 52, ty + 1);

                // 勾选框
                var box = new Rectangle(ax + 104, rr.Y + 10, 17, 17);
                if (sel) T.Fill(g, box, 4, T.Accent);
                T.Stroke(g, box, 4, sel ? T.Accent : T.LineStrong);
                if (sel)
                {
                    using (var p = new Pen(T.OnAccent, 2.2f))
                        g.DrawLines(p, new Point[] {
                            new Point(box.X + 4, box.Y + 8), new Point(box.X + 7, box.Y + 11), new Point(box.X + 13, box.Y + 4)
                        });
                }
                T.TextAt(g, sel ? "已选择" : "选择", T.FSmall, sel ? T.Accent : T.Muted, ax + 128, ty + 1);

                if (i < Items.Count - 1)
                    using (var p = new Pen(T.Line, 1f))
                        g.DrawLine(p, 12, rr.Bottom - 1, Width - 12, rr.Bottom - 1);
            }
        }
    }

    // ---------------------------------------------------------------- 日志（自绘，可截图）
    public class LogPanel : Control
    {
        public class Line
        {
            public string level;
            public string time;
            public string text;
        }

        public List<Line> Lines = new List<Line>();
        int scrollBack = 0;       // 向上滚动的行数
        const int LineH = 19;

        public LogPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.LogBg;
        }

        public void Clear()
        {
            Lines.Clear();
            scrollBack = 0;
            Invalidate();
        }

        public void Append(string level, string message)
        {
            var l = new Line();
            l.level = level;
            l.text = message;
            l.time = DateTime.Now.ToString("HH:mm:ss");
            Lines.Add(l);
            if (Lines.Count > 800) Lines.RemoveAt(0);
            scrollBack = 0;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int maxBack = Math.Max(0, Lines.Count - 1);
            scrollBack -= e.Delta / 120 * 2;
            if (scrollBack < 0) scrollBack = 0;
            if (scrollBack > maxBack) scrollBack = maxBack;
            Invalidate();
            base.OnMouseWheel(e);
        }

        static Color ColorFor(string level)
        {
            if (level == "ok") return T.Ok;
            if (level == "warn") return T.Warn;
            if (level == "error") return T.Danger;
            if (level == "chat") return T.Accent;
            if (level == "ai") return T.Violet;
            if (level == "info") return T.Dim;
            return T.Muted;
        }

        static string TagFor(string level)
        {
            if (level == "muted") return "     ";
            string t = level.ToUpper();
            if (t.Length > 5) t = t.Substring(0, 5);
            return t.PadRight(5);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var b = new SolidBrush(T.LogBg))
                g.FillRectangle(b, ClientRectangle);

            int visible = Height / LineH;
            int end = Lines.Count - scrollBack;
            int start = end - visible;
            if (start < 0) start = 0;

            int y = 2;
            for (int i = start; i < end && i < Lines.Count; i++)
            {
                var l = Lines[i];
                T.TextAt(g, l.time, T.FMonoSmall, T.LogTime, 2, y);
                T.TextAt(g, TagFor(l.level), T.FMonoSmall, ColorFor(l.level), 78, y);
                T.TextAt(g, l.text, T.FMonoSmall, ColorFor(l.level), 120, y);
                y += LineH;
            }
        }
    }

    // ---------------------------------------------------------------- 旧版日志（保留兼容）
    public class LogView : RichTextBox
    {
        public LogView()
        {
            ReadOnly = true;
            BorderStyle = BorderStyle.None;
            BackColor = Color.FromArgb(8, 13, 18);
            ForeColor = T.Dim;
            Font = T.FMonoSmall;
            DetectUrls = false;
            WordWrap = false;
            ScrollBars = RichTextBoxScrollBars.Vertical;
        }

        public void Append(string level, string message)
        {
            Color c = T.Dim;
            if (level == "ok") c = Color.FromArgb(198, 247, 221);
            else if (level == "warn") c = T.Amber;
            else if (level == "error") c = Color.FromArgb(255, 194, 203);
            else if (level == "chat") c = T.Blue;
            else if (level == "ai") c = Color.FromArgb(222, 215, 255);
            else if (level == "info") c = Color.FromArgb(150, 168, 186);

            string tag = level == "muted" ? "     " : level.ToUpper().PadRight(5);
            SelectionStart = TextLength;
            SelectionColor = Color.FromArgb(77, 91, 107);
            AppendText(DateTime.Now.ToString("HH:mm:ss") + "  ");
            SelectionStart = TextLength;
            SelectionColor = c;
            AppendText(tag + " ");
            SelectionStart = TextLength;
            SelectionColor = c;
            AppendText(message + "\n");
            SelectionStart = TextLength;
            ScrollToCaret();
        }
    }
}
