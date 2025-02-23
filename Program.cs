using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LspManager {

    public class Program {
        public static async Task Main(string[] argv) {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var cwd = Environment.CurrentDirectory;

            var config = await ConfigHelper.Load("config.toml");
            config.Destination = "D:\\porri\\Documents\\Projects\\LspManager";
            var t = config.ForEach(async (data) => {
                Console.WriteLine($"Fetching from url: {data.url} with target {data.target}");
                var response = await httpClient.GetStringAsync(data.url);
                using var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;
                string downloadUrl = null;
                string fileName = null;

                foreach (var asset in root.GetProperty("assets").EnumerateArray()) {
                    var name = asset.GetProperty("name").GetString();
                    if (name.Contains(data.target)) {
                        fileName = name;
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl)) {
                    Console.WriteLine($"Failed to find: {data.target}");
                    return;
                }

                var zipPath = Path.Combine(config.Destination, fileName);
                Console.WriteLine($"Zip Path: {zipPath}");

                using (var downloadStream = await httpClient.GetStreamAsync(downloadUrl)) {
                    using (var fileStream = File.Create(zipPath)) {
                        await downloadStream.CopyToAsync(fileStream);
                    }
                }
                Console.WriteLine($"Download completed for: {zipPath}");
                ZipFile.ExtractToDirectory(zipPath, config.Destination, true);
                File.Delete(zipPath);
            });

            t.Wait();
        }
    }
}
