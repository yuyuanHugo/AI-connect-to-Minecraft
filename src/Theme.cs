using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace McBot
{
    /// <summary>
    /// 浅蓝主题：页面浅蓝底 + 白色卡片 + 深蓝点缀
    /// </summary>
    public static class T
    {
        // ---- 底色 ----
        public static readonly Color Bg = Color.FromArgb(238, 244, 251);          // 页面底色（浅蓝）
        public static readonly Color BgTop = Color.FromArgb(255, 255, 255);       // 顶栏
        public static readonly Color Card = Color.FromArgb(255, 255, 255);        // 卡片
        public static readonly Color CardSoft = Color.FromArgb(246, 250, 254);    // 卡片内浅底
        public static readonly Color Input = Color.FromArgb(248, 251, 254);       // 输入框
        public static readonly Color Hover = Color.FromArgb(237, 244, 252);       // 悬停
        public static readonly Color RowSelected = Color.FromArgb(230, 240, 252); // 选中行

        // ---- 线条 ----
        public static readonly Color Line = Color.FromArgb(222, 232, 243);
        public static readonly Color LineStrong = Color.FromArgb(200, 215, 232);

        // ---- 文字 ----
        public static readonly Color Text = Color.FromArgb(23, 41, 61);
        public static readonly Color Dim = Color.FromArgb(73, 100, 124);
        public static readonly Color Muted = Color.FromArgb(128, 148, 166);

        // ---- 主色（深蓝） ----
        public static readonly Color Accent = Color.FromArgb(45, 122, 214);
        public static readonly Color AccentDeep = Color.FromArgb(30, 95, 180);
        public static readonly Color AccentLight = Color.FromArgb(108, 168, 232);
        public static readonly Color AccentSoft = Color.FromArgb(228, 239, 252);
        public static readonly Color OnAccent = Color.FromArgb(255, 255, 255);

        // ---- 语义色 ----
        public static readonly Color Ok = Color.FromArgb(24, 148, 96);
        public static readonly Color OkSoft = Color.FromArgb(228, 246, 238);
        public static readonly Color Warn = Color.FromArgb(190, 120, 20);
        public static readonly Color WarnSoft = Color.FromArgb(253, 245, 225);
        public static readonly Color Danger = Color.FromArgb(198, 62, 86);
        public static readonly Color DangerSoft = Color.FromArgb(253, 235, 238);

        // ---- 日志面板 ----
        public static readonly Color LogBg = Color.FromArgb(252, 253, 255);
        public static readonly Color LogTime = Color.FromArgb(160, 176, 192);

        // 兼容旧命名：原来的"绿色"现在就是主色蓝
        public static readonly Color Green = Accent;
        public static readonly Color GreenDeep = AccentDeep;
        public static readonly Color GreenLight = AccentLight;
        public static readonly Color Violet = Color.FromArgb(112, 96, 200);
        public static readonly Color Amber = Warn;
        public static readonly Color Blue = Accent;
        public static readonly Color Red = Danger;

        public static readonly Font FBase = new Font("Microsoft YaHei UI", 9F);
        public static readonly Font FSmall = new Font("Microsoft YaHei UI", 8.25F);
        public static readonly Font FTitle = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        public static readonly Font FCardTitle = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Bold);
        public static readonly Font FBig = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
        public static readonly Font FLaunch = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
        public static readonly Font FMono = new Font("Consolas", 9F);
        public static readonly Font FMonoSmall = new Font("Consolas", 8.25F);

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Fill(Graphics g, Rectangle r, int radius, Color c)
        {
            using (var path = Round(r, radius))
            using (var b = new SolidBrush(c))
                g.FillPath(b, path);
        }

        public static void Stroke(Graphics g, Rectangle r, int radius, Color c)
        {
            using (var path = Round(r, radius))
            using (var p = new Pen(c, 1f))
                g.DrawPath(p, path);
        }

        public static void FillGrad(Graphics g, Rectangle r, int radius, Color a, Color b)
        {
            using (var path = Round(r, radius))
            using (var br = new LinearGradientBrush(r, a, b, LinearGradientMode.ForwardDiagonal))
                g.FillPath(br, path);
        }

        public static void TextAt(Graphics g, string s, Font f, Color c, int x, int y)
        {
            using (var b = new SolidBrush(c))
                g.DrawString(s, f, b, x, y);
        }

        public static void TextAt(Graphics g, string s, Font f, Color c, int x, int y, int w, int h, StringAlignment hAlign)
        {
            var sf = new StringFormat();
            sf.Alignment = hAlign;
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming = StringTrimming.EllipsisCharacter;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            using (var b = new SolidBrush(c))
                g.DrawString(s, f, b, new RectangleF(x, y, w, h), sf);
            sf.Dispose();
        }

        public static void Badge(Graphics g, string text, Font f, Color fg, Color bg, Color border, int x, int y)
        {
            SizeF sz = g.MeasureString(text, f);
            int w = (int)sz.Width + 14;
            int h = (int)sz.Height + 4;
            var r = new Rectangle(x, y, w, h);
            Fill(g, r, h / 2, bg);
            Stroke(g, r, h / 2, border);
            TextAt(g, text, f, fg, x, y, w, h, StringAlignment.Center);
        }

        public static int MeasureBadge(Graphics g, string text, Font f)
        {
            return (int)g.MeasureString(text, f).Width + 14;
        }

        public static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
