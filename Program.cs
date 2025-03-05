using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BinGet;

public class Program {
    private const int BufferSize = 8192;

    public static async Task Main(string[] argv) {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        var config = await ConfigHelper.Load(argv[0]);
        var currentLine = Console.GetCursorPosition().Top;
        var buffer = new byte[config.Repositories.Count * BufferSize];

        if (currentLine + (4 * config.Repositories.Count) >= Console.BufferHeight) {
            Console.Clear();
            currentLine = Console.GetCursorPosition().Top;
        }

        var task = config.ForEach(async (data) => {
            var txtIdx = (data.index * 4) + currentLine;
            var progressBarIdx = txtIdx + 1;
            var extractedIdx = progressBarIdx + 1;

            Log($"[Url]: {data.url}, [Target]: {data.target}", txtIdx, ConsoleColor.White);
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
                Log($"[Url]: {data.url}, [Target]: {data.target}, [Status]: X", txtIdx, ConsoleColor.Red);
                return;
            }

            using var downloadResponse = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseContentRead);
            var status = downloadResponse.EnsureSuccessStatusCode();
            if (status.IsSuccessStatusCode) {
                var totalBytes = downloadResponse.Content.Headers.ContentLength ?? -1;
                var bytesRead = 0;
                long totalRead = 0;

                var ct = new CancellationToken();

                var zipPath = Path.Combine(config.Destination, fileName);
                Log($"[Url]: {data.url}, [Target]: {data.target}, [Zip]: {zipPath}", txtIdx, ConsoleColor.DarkYellow);

                using var contentStream = await downloadResponse.Content.ReadAsStreamAsync();
                var bufferSlice = buffer.AsMemory(data.index * BufferSize, BufferSize);
                using (var fileStream = new FileStream(
                            zipPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            BufferSize,
                            true)) {
                    while ((bytesRead = await contentStream.ReadAsync(bufferSlice, ct)) > 0) {
                        var readonlySlice = new ReadOnlyMemory<byte>(buffer, data.index * BufferSize, BufferSize);
                        await fileStream.WriteAsync(readonlySlice, ct);
                        totalRead += bytesRead;
                        DrawProgressBar(totalRead, totalBytes, progressBarIdx, downloadUrl);
                    }
                }

                var extractionPath = Path.Combine(config.Destination, data.display);
                ZipFile.ExtractToDirectory(zipPath, extractionPath, true);
                Log($"Cleaned up zip and extracted to: {extractionPath}", extractedIdx, ConsoleColor.Cyan);
                File.Delete(zipPath);
            } else {
                Log($"Failed to download: {downloadUrl}", extractedIdx, ConsoleColor.Red);
            }
        });

        task.Wait();
        var newLine = currentLine + (4 * config.Repositories.Count) + 1;
        Console.SetCursorPosition(0, newLine);
    }

    private static void Log(string msg, int line, ConsoleColor foreground) {
        if (line >= Console.BufferHeight) {
            return;
        }
        Console.ForegroundColor = foreground;
        Console.SetCursorPosition(0, line);
        Console.Write(msg);
    }

    private static void DrawProgressBar(long current, long total, int line, string url) {
        if (total <= 0 || line >= Console.BufferHeight) {
            return;
        }

        var barWidth = 50;
        var progress = (double)current / total;
        var progressBlocks = (int)(barWidth * progress);

        Console.CursorLeft = 0;
        Console.ForegroundColor = ConsoleColor.Blue;
        lock (Console.Out) {
            Console.SetCursorPosition(0, line);
            Console.Write($"{new string('█', progressBlocks)}{new string('-', barWidth - progressBlocks)} {progress * 100:0.0}% - {url}");
        }
    }
}
