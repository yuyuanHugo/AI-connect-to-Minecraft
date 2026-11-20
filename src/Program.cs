using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace McBot
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Runtime.Boot("Main args=" + string.Join(" ", args) + " dir=" + Runtime.Dir);
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                Runtime.Boot("FATAL " + ex);
                throw;
            }
        }

        static void Run(string[] args)
        {
            // .NET Framework 默认可能只启用旧版 TLS，Mojang / BMCLAPI 需要 TLS 1.2
            try
            {
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.DefaultConnectionLimit = 16;
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string shot = Arg(args, "--shot");
            if (shot != null)
            {
                RunShot(args, shot);
                return;
            }
            if (Flag(args, "--ensure-ovo") || Flag(args, "--ensure-runtime"))
            {
                bool ok = Runtime.EnsureOvo(null);
                Runtime.Boot("ensure-ovo result=" + ok);
                Environment.Exit(ok ? 0 : 1);
            }
            if (Flag(args, "--patch-ovo"))
            {
                OvoPatcher.EnsurePatched(Runtime.OvoDir, null);
                Environment.Exit(0);
            }
            if (Flag(args, "--selftest"))
            {
                Environment.Exit(SelfTest.Run() ? 0 : 1);
            }
            Application.Run(new MainForm());
        }

        static string Arg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        static bool Flag(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name) return true;
            return false;
        }

        static void RunShot(string[] args, string path)
        {
            bool launch = Arg(args, "--page") == "launch";
            bool status = Arg(args, "--page") == "status";
            string modalName = Arg(args, "--modal");
            bool modal = modalName == "versions" || modalName == "prompt";
            string filter = Arg(args, "--filter");

            var form = new MainForm();
            form.FormBorderStyle = FormBorderStyle.None;
            form.ClientSize = new Size(1180, 700);

            // 可选：指定截图尺寸，用来验证自适应布局（例如 --size 1000x640）
            string size = Arg(args, "--size");
            if (size != null)
            {
                var parts = size.Split('x');
                if (parts.Length == 2)
                {
                    int sw, sh;
                    if (int.TryParse(parts[0], out sw) && int.TryParse(parts[1], out sh))
                        form.ClientSize = new Size(sw, sh);
                }
            }

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, -4000);
            form.Show();
            Application.DoEvents();

            if (launch) form.ShowLaunchPage();
            if (status) form.ShowPage(2);
            if (Flag(args, "--tip")) form.SetForceTip(true);
            if (Flag(args, "--demo-logs")) form.FillDemoLogs();
            Application.DoEvents();
            Thread.Sleep(250);
            Application.DoEvents();

            if (modalName == "prompt")
            {
                var dlg = new PromptDialog(form.Cfg.ai.systemPrompt == null ? "" : form.Cfg.ai.systemPrompt,
                    "当前提供商：DeepSeek　留空则使用 OVO 自带人格提示词");
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(-4000, -4000);
                dlg.Show();
                Application.DoEvents();
                Thread.Sleep(200);
                Application.DoEvents();
                using (var bmp = new Bitmap(dlg.ClientSize.Width, dlg.ClientSize.Height))
                {
                    dlg.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                File.AppendAllText(Runtime.BootLog, "shot(prompt) " + path + Environment.NewLine);
                dlg.Close();
                Environment.Exit(0);
            }
            if (modal)
            {
                var dlg = new VersionDialog(form.Cfg.client.version);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.Location = new Point(-4000, -4000);
                if (filter == "april") dlg.SetFilter(3);
                if (filter == "release") dlg.SetFilter(1);
                dlg.Show();
                Application.DoEvents();
                Thread.Sleep(200);
                Application.DoEvents();
                Snap(dlg, path);
                dlg.Close();
            }
            else
            {
                Snap(form, path);
            }
            form.Close();
            Console.WriteLine("saved " + path);
        }

        static void Snap(Form f, string path)
        {
            using (var bmp = new Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(path, ImageFormat.Png);
            }
        }
    }
}
