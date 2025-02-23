using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Tomlyn;

namespace LspManager;

public static class ConfigHelper {
    public static async Task<Config> Load(string path) {
        var text = await File.ReadAllTextAsync(path);
        return Toml.Parse(text).ToModel<Config>();
    }

    public static Task ForEach(this Config config, [NotNull] Func<(string url, string target), Task> action) {
        if (!Directory.Exists(config.Destination)) {
            Directory.CreateDirectory(config.Destination);
        }

        var tasks = new Task[config.Repositories.Count];
        var index = 0;
        foreach (var setting in config.Repositories) {
            var url = $"{config.URL}{setting.Name}/releases/latest";

            var task = action.Invoke((url, setting.Target));
            tasks[index++] = task;
        }
        return Task.WhenAll(tasks);
    }
}

public class Config {
    [DataMember(Name = "url")]
    public string URL { get; set; }

    [DataMember(Name = "destination")]
    public string Destination { get; set; }

    [DataMember(Name = "repositories")]
    public List<Setting> Repositories { get; set; }
}

public class Setting {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "target")]
    public string Target { get; set; }
}