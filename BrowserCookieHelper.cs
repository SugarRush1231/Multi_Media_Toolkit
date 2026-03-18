using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace YoutubeDownloader
{
    public static class BrowserCookieHelper
    {
        public static string GenerateNetscapeCookies()
        {
            var task = Task.Run(async () => await GenerateNetscapeCookiesAsync());
            task.Wait();
            return task.Result;
        }

        private static async Task<string> GenerateNetscapeCookiesAsync()
        {
            string bestBrowser = "none";
            string bestProfile = "";
            string exePath = "";
            DateTime latestTime = DateTime.MinValue;

            var configs = new[] 
            {
                new { name = "chrome", exe = @"Google\Chrome\Application\chrome.exe", profile = @"Google\Chrome\User Data" },
                new { name = "edge", exe = @"Microsoft\Edge\Application\msedge.exe", profile = @"Microsoft\Edge\User Data" },
                new { name = "brave", exe = @"BraveSoftware\Brave-Browser\Application\brave.exe", profile = @"BraveSoftware\Brave-Browser\User Data" },
                new { name = "whale", exe = @"Naver\Naver Whale\Application\whale.exe", profile = @"Naver\Naver Whale\User Data" }
            };

            foreach (var cfg in configs)
            {
                string pFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string pFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string fullExe = Path.Combine(pFiles, cfg.exe);
                if (!File.Exists(fullExe)) fullExe = Path.Combine(pFiles86, cfg.exe);
                if (!File.Exists(fullExe)) fullExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), cfg.exe);
                
                if (!File.Exists(fullExe)) continue;

                string userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), cfg.profile);
                if (!Directory.Exists(userData)) continue;

                var dirs = new List<string>(Directory.GetDirectories(userData, "Profile *"));
                dirs.Add(Path.Combine(userData, "Default"));

                foreach (var dir in dirs)
                {
                    string cookiePath = Path.Combine(dir, "Network", "Cookies");
                    if (!File.Exists(cookiePath)) cookiePath = Path.Combine(dir, "Cookies");
                    if (File.Exists(cookiePath))
                    {
                        var fi = new FileInfo(cookiePath);
                        if (fi.LastWriteTime > latestTime)
                        {
                            latestTime = fi.LastWriteTime;
                            bestBrowser = cfg.name;
                            bestProfile = dir;
                            exePath = fullExe;
                        }
                    }
                }
            }

            if (bestBrowser == "none" || !File.Exists(exePath)) return "none";

            try
            {
                // 실시간 복제
                string tempBase = Path.Combine(Path.GetTempPath(), "MMT_CDP_" + DateTime.Now.Ticks);
                Directory.CreateDirectory(tempBase);
                string tempProfile = Path.Combine(tempBase, "Default");
                Directory.CreateDirectory(Path.Combine(tempProfile, "Network"));

                string srcCookies = Path.Combine(bestProfile, "Network", "Cookies");
                if (!File.Exists(srcCookies)) srcCookies = Path.Combine(bestProfile, "Cookies");
                if (File.Exists(srcCookies)) File.Copy(srcCookies, Path.Combine(tempProfile, "Network", "Cookies"), true);
                
                string localState = Path.Combine(Directory.GetParent(bestProfile).FullName, "Local State");
                if (File.Exists(localState)) File.Copy(localState, Path.Combine(tempBase, "Local State"), true);

                // CDP 활성화하여 무두 브라우저 백그라운드 실행
                int port = 9222 + new Random().Next(0, 1000);
                var psi = new ProcessStartInfo(exePath, $"--headless=new --remote-debugging-port={port} --user-data-dir=\"{tempBase}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                await Task.Delay(2000); // 실행 대기

                using var hc = new HttpClient();
                hc.Timeout = TimeSpan.FromSeconds(5);
                string jsonResponse = await hc.GetStringAsync($"http://localhost:{port}/json");
                using var doc = JsonDocument.Parse(jsonResponse);
                string wsUrl = doc.RootElement[0].GetProperty("webSocketDebuggerUrl").GetString();

                // WebSocket을 통해 쿠키 전체 덤프
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                string req = "{\"id\": 1, \"method\": \"Network.getAllCookies\"}";
                var reqBytes = Encoding.UTF8.GetBytes(req);
                await ws.SendAsync(new ArraySegment<byte>(reqBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                var buffer = new byte[1024 * 1024 * 2]; // 2MB
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string responseStr = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                process.Kill();
                try { Directory.Delete(tempBase, true); } catch { }

                // Netscape 포맷 변환
                using var resDoc = JsonDocument.Parse(responseStr);
                string txtPath = Path.Combine(Path.GetTempPath(), "mmt_cookies.txt");
                var sb = new StringBuilder();
                sb.AppendLine("# Netscape HTTP Cookie File");

                if (resDoc.RootElement.TryGetProperty("result", out var resToken) && resToken.TryGetProperty("cookies", out var cookiesObj))
                {
                    foreach (var c in cookiesObj.EnumerateArray())
                    {
                        string domain = c.GetProperty("domain").GetString();
                        bool flag1 = domain.StartsWith(".");
                        string path = c.GetProperty("path").GetString();
                        bool secure = c.TryGetProperty("secure", out var sec) ? sec.GetBoolean() : false;
                        long expires = 0;
                        if (c.TryGetProperty("expires", out var exp)) expires = (long)exp.GetDouble();
                        string name = c.GetProperty("name").GetString();
                        string value = c.GetProperty("value").GetString();

                        sb.AppendLine($"{domain}\t{(flag1 ? "TRUE" : "FALSE")}\t{path}\t{(secure ? "TRUE" : "FALSE")}\t{expires}\t{name}\t{value}");
                    }
                }

                File.WriteAllText(txtPath, sb.ToString());
                return txtPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CDP Cookie Export Error: " + ex.Message);
                return "none";
            }
        }
    }
}
