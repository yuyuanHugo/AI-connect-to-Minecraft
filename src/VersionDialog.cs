using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace McBot
{
    public class VersionDialog : Form
    {
        public string SelectedVersion = "";
        InputBox search;
        Segmented filter;
        ListBox list;
        Label count;
        List<VersionInfo> shown = new List<VersionInfo>();

        public VersionDialog(string current)
        {
            Text = "选择版本";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 600);
            BackColor = T.Card;
            ShowInTaskbar = false;

            var title = new Label();
            title.Text = "选择版本";
            title.Font = T.FCardTitle;
            title.ForeColor = T.Text;
            title.SetBounds(16, 14, 300, 22);
            Controls.Add(title);

            search = new InputBox();
            search.SetBounds(16, 46, 488, 32);
            search.Inner.TextChanged += delegate { Refilter(); };
            Controls.Add(search);

            filter = new Segmented();
            filter.Items = new string[] { "全部", "正式版", "快照", "愚人节" };
            filter.Font = T.FSmall;
            filter.SetBounds(16, 86, 300, 32);
            filter.Changed += delegate { Refilter(); };
            Controls.Add(filter);

            var holder = new Panel();
            holder.SetBounds(16, 126, 488, 380);
            holder.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                T.Fill(e.Graphics, new Rectangle(0, 0, holder.Width - 1, holder.Height - 1), 8, T.Input);
                T.Stroke(e.Graphics, new Rectangle(0, 0, holder.Width - 1, holder.Height - 1), 8, T.LineStrong);
            };
            Controls.Add(holder);

            list = new ListBox();
            list.BorderStyle = BorderStyle.None;
            list.BackColor = T.Input;
            list.ForeColor = T.Text;
            list.Font = T.FMonoSmall;
            list.DrawMode = DrawMode.OwnerDrawFixed;
            list.ItemHeight = 30;
            list.IntegralHeight = false;
            list.SetBounds(4, 4, 480, 372);
            list.DrawItem += DrawItem;
            list.DoubleClick += delegate { Accept(); };
            holder.Controls.Add(list);

            count = new Label();
            count.Font = T.FSmall;
            count.ForeColor = T.Muted;
            count.SetBounds(16, 518, 240, 20);
            Controls.Add(count);

            var cancel = new FlatButton();
            cancel.Text = "取消";
            cancel.SetBounds(290, 514, 92, 34);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var ok = new FlatButton();
            ok.Text = "使用此版本";
            ok.Kind = BtnKind.Primary;
            ok.SetBounds(392, 514, 112, 34);
            ok.Enabled = false;
            ok.Click += delegate { Accept(); };
            Controls.Add(ok);
            list.SelectedIndexChanged += delegate { ok.Enabled = list.SelectedIndex >= 0; };

            SelectedVersion = current;
            Refilter();
            SelectCurrent(current);
        }

        void SelectCurrent(string id)
        {
            for (int i = 0; i < shown.Count; i++)
            {
                if (shown[i].id == id) { list.SelectedIndex = i; return; }
            }
        }

        public void SetFilter(int idx)
        {
            filter.SelectedIndex = idx;
            Refilter();
        }

        void Accept()
        {
            if (list.SelectedIndex >= 0 && list.SelectedIndex < shown.Count)
            {
                SelectedVersion = shown[list.SelectedIndex].id;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        void Refilter()
        {
            string q = search.Text.Trim().ToLower();
            int mode = filter.SelectedIndex;
            shown = new List<VersionInfo>();
            var all = VersionRepo.Items;
            for (int i = 0; i < all.Count && shown.Count < 400; i++)
            {
                var v = all[i];
                if (mode == 1 && v.type != "release") continue;
                if (mode == 2 && v.type != "snapshot") continue;
                if (mode == 3 && !v.april) continue;
                if (q.Length > 0 && v.id.ToLower().IndexOf(q) < 0) continue;
                shown.Add(v);
            }
            list.BeginUpdate();
            list.Items.Clear();
            for (int i = 0; i < shown.Count; i++) list.Items.Add(shown[i].id);
            list.EndUpdate();
            count.Text = string.Format("{0} 个版本{1}", shown.Count, shown.Count >= 400 ? "（显示前 400）" : "");
        }

        void DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= shown.Count) return;
            var v = shown[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool sel = (e.State & DrawItemState.Selected) != 0;
            Color bg = sel ? Color.FromArgb(22, 46, 34) : T.Input;
            using (var b = new SolidBrush(bg)) g.FillRectangle(b, e.Bounds);
            if (v.id == SelectedVersion)
                using (var b = new SolidBrush(Color.FromArgb(28, 61, 220, 132)))
                    g.FillRectangle(b, e.Bounds);

            T.TextAt(g, v.id, T.FMono, T.Text, e.Bounds.X + 10, e.Bounds.Y + 7);

            Color fg = v.april ? T.Amber : (v.type == "release" ? T.Green : T.Blue);
            Color bgb = Color.FromArgb(28, fg.R, fg.G, fg.B);
            int bx = e.Bounds.X + 10 + (int)g.MeasureString(v.id, T.FMono).Width + 10;
            T.Badge(g, v.BadgeText, T.FSmall, fg, bgb, Color.FromArgb(90, fg.R, fg.G, fg.B), bx, e.Bounds.Y + 6);

            string when = v.releaseTime.Length >= 10 ? v.releaseTime.Substring(0, 10) : v.releaseTime;
            T.TextAt(g, when, T.FMonoSmall, T.Muted, e.Bounds.Right - 110, e.Bounds.Y, 100, e.Bounds.Height, StringAlignment.Far);
        }
    }
}
