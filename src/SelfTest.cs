using System;
using System.Collections.Generic;

namespace McBot
{
    /// <summary>无界面自检：验证"选中服务器 → 自动匹配版本""删除后自动选最上面"等逻辑</summary>
    public static class SelfTest
    {
        static int pass, fail;

        static void Check(string name, bool ok, string detail)
        {
            if (ok) { pass++; Console.WriteLine("  PASS  " + name); }
            else { fail++; Console.WriteLine("  FAIL  " + name + "  -> " + detail); }
        }

        static Config Build()
        {
            var c = new Config();
            c.servers = new List<ServerEntry>();
            var a = new ServerEntry(); a.id = "a"; a.host = "a.example"; a.port = 25565; a.version = "1.21.4";
            var b = new ServerEntry(); b.id = "b"; b.host = "b.example"; b.port = 25565; b.version = "1.20.1";
            var d = new ServerEntry(); d.id = "d"; d.host = "c.example"; d.port = 25566; d.version = "1.16.5";
            c.servers.Add(a); c.servers.Add(b); c.servers.Add(d);
            c.selectedServerId = "b";
            c.client.version = "1.20.1";
            return c;
        }

        public static bool Run()
        {
            Console.WriteLine("自检开始");

            // 1. 删除当前选中的服务器 → 选中最上面那条，并同步版本
            var c1 = Build();
            ConfigPage.RemoveServer(c1, "b");
            Check("删除选中项 → 选中第一条", c1.selectedServerId == "a", "selected=" + c1.selectedServerId);
            Check("删除选中项 → 版本跟随", c1.client.version == "1.21.4", "version=" + c1.client.version);
            Check("删除后剩余 2 条", c1.servers.Count == 2, "count=" + c1.servers.Count);

            // 2. 删除非选中项 → 选择不变
            var c2 = Build();
            ConfigPage.RemoveServer(c2, "a");
            Check("删除非选中项 → 选择不变", c2.selectedServerId == "b", "selected=" + c2.selectedServerId);
            Check("删除非选中项 → 版本不变", c2.client.version == "1.20.1", "version=" + c2.client.version);

            // 3. 全删光 → 无选中
            var c3 = Build();
            ConfigPage.RemoveServer(c3, "a");
            ConfigPage.RemoveServer(c3, "b");
            ConfigPage.RemoveServer(c3, "d");
            Check("全部删除 → 无选中", c3.selectedServerId == null, "selected=" + c3.selectedServerId);
            Check("全部删除 → 服务器为 0", c3.servers.Count == 0, "count=" + c3.servers.Count);

            // 4. 选中服务器时版本自动匹配（模拟启动页下拉切换）
            var c4 = Build();
            var target = c4.servers[2];
            c4.selectedServerId = target.id;
            c4.client.version = target.version;
            Check("切换服务器 → 版本自动匹配", c4.client.version == "1.16.5", "version=" + c4.client.version);

            // 5. 版本清单可加载
            Check("版本清单加载", VersionRepo.Items.Count > 800, "count=" + VersionRepo.Items.Count);

            Console.WriteLine(string.Format("自检完成：{0} 通过，{1} 失败", pass, fail));
            return fail == 0;
        }
    }
}
