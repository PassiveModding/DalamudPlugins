using System;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHubApiExample
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            HttpClient client = new HttpClient();
            // set user agent to avoid 403
            client.DefaultRequestHeaders.Add("User-Agent", "request");
            List<JObject> pluginsOut = new List<JObject>();

            string pluginList = File.ReadAllText("./repos.json");
            var plugins = JsonConvert.DeserializeObject<List<JObject>>(pluginList);
            if (plugins == null) return;

            foreach (JObject plugin in plugins)
            {
                if (plugin == null) continue;
                
                string username = plugin.GetValue("username", StringComparison.OrdinalIgnoreCase)?.ToString() ?? "";
                string repo = plugin.GetValue("repo", StringComparison.OrdinalIgnoreCase)?.ToString() ?? "";
                string branch = plugin.GetValue("branch", StringComparison.OrdinalIgnoreCase)?.ToString() ?? "master";
                string configPath = plugin.GetValue("configPath", StringComparison.OrdinalIgnoreCase)?.ToString() ?? "plugin.json";

                HttpResponseMessage data = await client.GetAsync($"https://api.github.com/repos/{username}/{repo}/releases/latest");
                var responseContent = await data.Content.ReadAsStringAsync();
                if (responseContent == null) continue;
                JObject? latestRelease = JsonConvert.DeserializeObject<JObject>(responseContent);
                if (latestRelease == null) continue;

                int? count = (int?)latestRelease["assets"]?[0]?["download_count"];
                string? assembly = (string?)latestRelease["tag_name"];
                string? download = (string?)latestRelease["assets"]?[0]?["browser_download_url"];
                DateTime? publishedAt = (DateTime?)latestRelease["published_at"];
                int? time = (int?)(publishedAt - new DateTime(1970, 1, 1))?.TotalSeconds;
                if (count == null || assembly == null || download == null || time == null) continue;

                string configUrl = $"https://raw.githubusercontent.com/{username}/{repo}/{branch}/{configPath}";
                HttpResponseMessage configData = await client.GetAsync(configUrl);
                var configContent = await configData.Content.ReadAsStringAsync();
                if (configContent == null) continue;
                JObject? config = JsonConvert.DeserializeObject<JObject>(configContent);
                if (config == null) continue;

                config["IsHide"] = "False";
                config["IsTestingExclusive"] = "False";
                config["AssemblyVersion"] = assembly;
                config["LastUpdated"] = time;
                config["DownloadCount"] = count;
                config["DownloadLinkInstall"] = download;
                config["DownloadLinkTesting"] = download;
                config["DownloadLinkUpdate"] = download;

                pluginsOut.Add(config);
            }

            string pluginJson = JsonConvert.SerializeObject(pluginsOut, Formatting.Indented);
            File.WriteAllText("./repo.json", pluginJson);
        }
    }
}
