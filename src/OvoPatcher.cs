using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace McBot
{
    /// <summary>
    /// 给内嵌的 DeepseekOVO 源码打启动器补丁（幂等）：
    /// 1. 私信回复 / 公屏@回复（读 reply-mode.txt，切换即时生效）
    /// 2. 去掉网页面板（mindserver 只当内部总线）
    /// 3. 把对话 / 状态 / 背包用 DATA| 协议推给启动器
    /// OVO 其它逻辑不动。
    /// </summary>
    public static class OvoPatcher
    {
        const string NL = "\r\n";

        public static void EnsurePatched(string ovoDir, Action<string> log)
        {
            try
            {
                PatchAgent(Path.Combine(ovoDir, "src\\agent\\agent.js"), log);
                PatchMindserver(Path.Combine(ovoDir, "src\\mindcraft\\mindserver.js"), log);
            }
            catch (Exception ex)
            {
                if (log != null) log("OVO 补丁失败：" + ex.Message);
            }
        }

        static void PatchAgent(string path, Action<string> log)
        {
            if (!File.Exists(path)) return;
            string s = File.ReadAllText(path);
            bool changed = false;

            // 0) 需要 fs 读 reply-mode.txt
            if (!s.Contains("import * as __fs from 'fs';"))
            {
                int nl = s.IndexOf('\n');
                if (nl > 0)
                {
                    s = s.Insert(nl + 1, "import * as __fs from 'fs';" + NL);
                    changed = true;
                }
            }

            // 0.1) 文件桥：把 DATA| 行同时写进 bridge.log，启动器轮询读取
            if (!s.Contains("__launcherBridge"))
            {
                string bridge =
                    "// [启动器补丁] 文件桥：DATA| 行同时落盘，启动器直接读文件，不依赖 stdout" + NL +
                    "const __origLog = console.log;" + NL +
                    "console.log = function (...__args) {" + NL +
                    "    try {" + NL +
                    "        const __a = __args[0];" + NL +
                    "        if (typeof __a === 'string' && __a.indexOf('DATA|') === 0) {" + NL +
                    "            const __p = process.cwd() + '/bridge.log';" + NL +
                    "            let __sz = 0;" + NL +
                    "            try { __sz = __fs.statSync(__p).size; } catch (e) { }" + NL +
                    "            if (__sz > 4 * 1024 * 1024) { __fs.writeFileSync(__p, ''); }" + NL +
                    "            __fs.appendFileSync(__p, __a + '\\n');" + NL +
                    "        }" + NL +
                    "    } catch (e) { }" + NL +
                    "    return __origLog.apply(console, __args);" + NL +
                    "};" + NL +
                    "const __launcherBridge = true;" + NL;
                int insertAt = s.IndexOf("import * as __fs from 'fs';");
                if (insertAt > 0)
                {
                    int lineEnd = s.IndexOf('\n', insertAt) + 1;
                    s = s.Insert(lineEnd, bridge);
                    changed = true;
                }
            }

            // 1) 记录私信对象 + 上报收到的私信
            if (!s.Contains("this.whisper_to = username;"))
            {
                string anchor = "console.log(this.name, 'received message from', username, ':', message);";
                if (s.Contains(anchor))
                {
                    s = ReplaceFirst(s, anchor,
                        anchor + NL +
                        "                this.whisper_to = username;" + NL +
                        "                try { console.log('DATA|chat|' + JSON.stringify({ from: 'user', name: username, text: String(message), channel: 'whisper' })); } catch (e) { }");
                    changed = true;
                }
            }

            // 1.5) 回应范围：所有人 / 仅白名单
            if (!s.Contains("不在白名单"))
            {
                string anchor2 = "            if (message === \"\") return;";
                int ai = s.IndexOf(anchor2, StringComparison.Ordinal);
                if (ai > 0)
                {
                    int lineEnd = s.IndexOf('\n', ai);
                    if (lineEnd > 0)
                    {
                        string eol = (lineEnd > 0 && s[lineEnd - 1] == '\r') ? "\r\n" : "\n";
                        string guard =
                            eol +
                            "            // [启动器补丁] 回应范围（所有人 / 仅白名单），读 respond-scope.txt 实时生效" + eol +
                            "            try {" + eol +
                            "                let __scope = 'all';" + eol +
                            "                try { __scope = (__fs.readFileSync(process.cwd() + '/respond-scope.txt', 'utf8') || '').trim() || 'all'; } catch (e) { }" + eol +
                            "                if (__scope === 'whitelist') {" + eol +
                            "                    let __names = [];" + eol +
                            "                    try { __names = (__fs.readFileSync(process.cwd() + '/whitelist.txt', 'utf8') || '').split(/[,\\n;]/).map((x) => x.trim().toLowerCase()).filter(Boolean); } catch (e) { }" + eol +
                            "                    const __u = String(username).toLowerCase();" + eol +
                            "                    if (__names.indexOf(__u) < 0) {" + eol +
                            "                        try { console.log('DATA|chat|' + JSON.stringify({ from: 'system', text: '(' + username + ' 不在白名单，未回应) ' + message, channel: 'muted' })); } catch (e) { }" + eol +
                            "                        return;" + eol +
                            "                    }" + eol +
                            "                }" + eol +
                            "            } catch (e) { }";
                        s = s.Insert(lineEnd + 1, guard.TrimStart('\r', '\n') + eol);
                        changed = true;
                    }
                }
            }

            // 2) 公屏聊天：私信模式忽略；公屏模式被 @ 才回应
            string modeHandler =
                "        // [启动器补丁] 私信 / 公屏@ 两种模式（读 reply-mode.txt，实时生效）" + NL +
                "        this.bot.on('chat', (username, message) => {" + NL +
                "            if (username === this.bot.username) return;" + NL +
                "            let __mode = 'whisper';" + NL +
                "            try { __mode = (__fs.readFileSync(process.cwd() + '/reply-mode.txt', 'utf8') || '').trim() || 'whisper'; } catch (e) { }" + NL +
                "            const __mentioned = !!message && (" + NL +
                "                message.toLowerCase().includes(String(this.bot.username).toLowerCase()) ||" + NL +
                "                message.trim().startsWith('@')" + NL +
                "            );" + NL +
                "            if (__mode === 'public' && __mentioned) {" + NL +
                "                this.whisper_to = username;" + NL +
                "                respondFunc(username, message);" + NL +
                "                return;" + NL +
                "            }" + NL +
                "            try { console.log('DATA|chat|' + JSON.stringify({ from: 'system', text: '(' + (__mode === 'public' ? '公屏未点名' : '公屏已忽略') + ') ' + username + ': ' + message, channel: 'muted' })); } catch (e) { }" + NL +
                "        });";

            if (!s.Contains("私信 / 公屏@ 两种模式"))
            {
                int ci = s.IndexOf("this.bot.on('chat'", StringComparison.Ordinal);
                if (ci > 0)
                {
                    int end = s.IndexOf("});", ci, StringComparison.Ordinal);
                    if (end > 0)
                    {
                        // 连同上面那行注释一起换掉（如果它是我们之前加的）
                        int lineStart = s.LastIndexOf('\n', ci) + 1;
                        int start = lineStart;
                        int prevBreak = s.LastIndexOf('\n', Math.Max(0, lineStart - 2));
                        if (prevBreak >= 0)
                        {
                            string prevLine = s.Substring(prevBreak + 1, lineStart - prevBreak - 1);
                            if (prevLine.Contains("启动器补丁")) start = prevBreak + 1;
                        }
                        s = s.Substring(0, start) + modeHandler + s.Substring(end + 3);
                        changed = true;
                    }
                }
            }

            // 3) 回复落点：私信 or 公屏 @
            string modeReply =
                "            let __rmode = 'whisper';" + NL +
                "            try { __rmode = (__fs.readFileSync(process.cwd() + '/reply-mode.txt', 'utf8') || '').trim() || 'whisper'; } catch (e) { }" + NL +
                "            if (this.whisper_to && __rmode === 'public') {" + NL +
                "                const __txt = '@' + this.whisper_to + ' ' + message;" + NL +
                "                try { console.log('DATA|chat|' + JSON.stringify({ from: 'bot', name: this.name, text: __txt, channel: 'public' })); } catch (e) { }" + NL +
                "                try { this.bot.chat(__txt); } catch (e) { }" + NL +
                "            } else if (this.whisper_to) {" + NL +
                "                try { console.log('DATA|chat|' + JSON.stringify({ from: 'bot', name: this.name, text: message, channel: 'whisper' })); } catch (e) { }" + NL +
                "                try { this.bot.whisper(this.whisper_to, message); } catch (e) { }" + NL +
                "            } else if (settings.chat_ingame) { this.bot.chat(message); }";

            if (!s.Contains("__rmode"))
            {
                string oldReply =
                    "            try { console.log('DATA|chat|' + JSON.stringify({ from: 'bot', name: this.name, text: message, channel: 'whisper' })); } catch (e) { }" + NL +
                    "            // [启动器补丁] 有私信对象就回私信，否则保持原逻辑" + NL +
                    "            if (this.whisper_to) { try { this.bot.whisper(this.whisper_to, message); } catch (e) { } }" + NL +
                    "            else if (settings.chat_ingame) {this.bot.chat(message);}";
                string oldPlain = "if (settings.chat_ingame) {this.bot.chat(message);}";
                if (s.Contains(oldReply)) { s = ReplaceFirst(s, oldReply, modeReply); changed = true; }
                else
                {
                    int pi = s.IndexOf(oldPlain, StringComparison.Ordinal);
                    if (pi >= 0)
                    {
                        // 如果它是 else / else if 的尾巴，把 else 一起吃掉，否则会留下悬空 else
                        bool hasElse = pi >= 5 && s.Substring(pi - 5, 5) == "else ";
                        if (hasElse)
                        {
                            s = s.Substring(0, pi - 5) + modeReply + s.Substring(pi + oldPlain.Length);
                        }
                        else
                        {
                            s = s.Substring(0, pi) + modeReply + s.Substring(pi + oldPlain.Length);
                        }
                        changed = true;
                    }
                }
            }

            // 3.5) 清掉上一版残留的私信分支和重复日志（否则会重复发送）
            if (s.Contains("__rmode"))
            {
                string before = s;
                s = Regex.Replace(s, @"[ \t]*// \[启动器补丁\] 有私信对象就回私信，否则保持原逻辑\r?\n[ \t]*if \(this\.whisper_to\)[^\n]*\r?\n", "");
                s = Regex.Replace(s, @"[ \t]*try \{ console\.log\('DATA\|chat\|' \+ JSON\.stringify\(\{ from: 'bot', name: this\.name, text: message, channel: 'whisper' \}\)\); \} catch \(e\) \{ \}\r?\n(?=[ \t]*let __rmode)", "");
                if (s != before) changed = true;
            }
            // 4) 状态 + 背包推送
            string events = "    startEvents() {";
            if (s.Contains(events))
            {
                var sb = new StringBuilder();
                if (!s.Contains("DATA|state|"))
                {
                    sb.Append(NL + "        // [启动器补丁] 状态推给启动器" + NL);
                    sb.Append("        setInterval(() => {" + NL);
                    sb.Append("            try {" + NL);
                    sb.Append("                const b = this.bot;" + NL);
                    sb.Append("                if (!b || !b.entity) return;" + NL);
                    sb.Append("                const p = b.entity.position;" + NL);
                    sb.Append("                console.log('DATA|state|' + JSON.stringify({" + NL);
                    sb.Append("                    in_game: true, phase: 'online'," + NL);
                    sb.Append("                    pos: { x: Math.round(p.x), y: Math.round(p.y), z: Math.round(p.z) }," + NL);
                    sb.Append("                    health: typeof b.health === 'number' ? Math.round(b.health) : null," + NL);
                    sb.Append("                    food: typeof b.food === 'number' ? Math.round(b.food) : null," + NL);
                    sb.Append("                    players: Object.keys(b.players || {}).filter((n) => n !== b.username)," + NL);
                    sb.Append("                    frozen: false, selfAware: true," + NL);
                    sb.Append("                    gameMode: b.game ? b.game.gameMode : null" + NL);
                    sb.Append("                }));" + NL);
                    sb.Append("            } catch (e) { }" + NL);
                    sb.Append("        }, 5000);" + NL);
                }
                if (!s.Contains("DATA|inventory|"))
                {
                    sb.Append(NL + "        // [启动器补丁] 背包推给启动器" + NL);
                    sb.Append("        setInterval(() => {" + NL);
                    sb.Append("            try {" + NL);
                    sb.Append("                const b = this.bot;" + NL);
                    sb.Append("                if (!b || !b.entity) return;" + NL);
                    sb.Append("                const items = b.inventory.items().map((i) => ({ name: i.name, count: i.count, slot: i.slot }));" + NL);
                    sb.Append("                const off = b.inventory.slots[45];" + NL);
                    sb.Append("                console.log('DATA|inventory|' + JSON.stringify({ items: items, offhand: off ? { name: off.name, count: off.count } : null }));" + NL);
                    sb.Append("            } catch (e) { }" + NL);
                    sb.Append("        }, 5000);" + NL);
                }
                if (sb.Length > 0)
                {
                    s = ReplaceFirst(s, events, events + sb.ToString());
                    changed = true;
                }
            }

            if (changed)
            {
                File.WriteAllText(path, s, new UTF8Encoding(false));
                if (log != null) log("已给 OVO 源码打好启动器补丁");
            }
        }

        static void PatchMindserver(string path, Action<string> log)
        {
            if (!File.Exists(path)) return;
            string s = File.ReadAllText(path);
            string oldLine = "app.use(express.static(path.join(__dirname, 'public')));";
            if (s.Contains(oldLine))
            {
                s = ReplaceFirst(s, oldLine,
                    "// [启动器补丁] 不再提供网页面板，mindserver 只当内部总线" + NL + "// " + oldLine);
                File.WriteAllText(path, s, new UTF8Encoding(false));
                if (log != null) log("已移除 OVO 网页面板");
            }
        }

        static string ReplaceFirst(string text, string search, string replace)
        {
            int i = text.IndexOf(search, StringComparison.Ordinal);
            if (i < 0) return text;
            return text.Substring(0, i) + replace + text.Substring(i + search.Length);
        }
    }
}
