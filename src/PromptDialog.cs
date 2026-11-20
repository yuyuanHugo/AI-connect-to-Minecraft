using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace McBot
{
    /// <summary>编辑 AI 人格提示词的弹窗</summary>
    public class PromptDialog : Form
    {
        public string PromptText = "";
        TextBox editor;
        Label hint;

        public PromptDialog(string current, string providerHint)
        {
            Text = "编辑提示词";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 560);
            MinimumSize = new Size(560, 400);
            BackColor = T.Bg;
            Font = T.FBase;
            DoubleBuffered = true;

            var title = new Label();
            title.Text = "AI 人格提示词";
            title.Font = T.FTitle;
            title.ForeColor = T.Text;
            title.BackColor = Color.Transparent;
            title.SetBounds(24, 18, 300, 28);
            Controls.Add(title);

            hint = new Label();
            hint.Text = providerHint;
            hint.Font = T.FSmall;
            hint.ForeColor = T.Muted;
            hint.BackColor = Color.Transparent;
            hint.SetBounds(24, 48, ClientSize.Width - 48, 20);
            Controls.Add(hint);

            editor = new TextBox();
            editor.Multiline = true;
            editor.ScrollBars = ScrollBars.Vertical;
            editor.WordWrap = true;
            editor.BorderStyle = BorderStyle.FixedSingle;
            editor.BackColor = T.Input;
            editor.ForeColor = T.Text;
            editor.Font = T.FMono;
            editor.Text = current == null ? "" : current;
            editor.SetBounds(24, 76, ClientSize.Width - 48, ClientSize.Height - 76 - 72);
            Controls.Add(editor);

            var ok = new FlatButton();
            ok.Text = "保存";
            ok.Kind = BtnKind.Primary;
            ok.SetBounds(ClientSize.Width - 24 - 108, ClientSize.Height - 56, 108, 36);
            ok.Click += delegate { PromptText = editor.Text; DialogResult = DialogResult.OK; Close(); };
            Controls.Add(ok);

            var cancel = new FlatButton();
            cancel.Text = "取消";
            cancel.Kind = BtnKind.Ghost;
            cancel.SetBounds(ClientSize.Width - 24 - 108 - 12 - 108, ClientSize.Height - 56, 108, 36);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var clear = new FlatButton();
            clear.Text = "清空（用 OVO 默认人格）";
            clear.Kind = BtnKind.Ghost;
            clear.SetBounds(24, ClientSize.Height - 56, 220, 36);
            clear.Click += delegate { editor.Text = ""; };
            Controls.Add(clear);

            Resize += delegate
            {
                editor.SetBounds(24, 76, ClientSize.Width - 48, ClientSize.Height - 76 - 72);
                hint.SetBounds(24, 48, ClientSize.Width - 48, 20);
                ok.SetBounds(ClientSize.Width - 24 - 108, ClientSize.Height - 56, 108, 36);
                cancel.SetBounds(ClientSize.Width - 24 - 108 - 12 - 108, ClientSize.Height - 56, 108, 36);
                clear.SetBounds(24, ClientSize.Height - 56, 220, 36);
            };
        }
    }
}
