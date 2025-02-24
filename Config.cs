using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace LspManager;

public static class ConfigHelper {
#if !IL
    static string GetTomlStringValue(DocumentSyntax document, string key) {
        if (document.ToModel().TryGetValue(key, out var value)) {
            return value.ToString();
        }
        return null;
    }
#endif

    public static async Task<Config> Load(string path) {
        var text = await File.ReadAllTextAsync(path);
        var document = Toml.Parse(text);
#if IL
        return Toml.Parse(text).ToModel<Config>();
#else
        var config = new Config();

        // Manually parsing to avoid reflection model
        config.URL = GetTomlStringValue(document, "url");
        config.Destination = GetTomlStringValue(document, "destination");

        if (document.ToModel().TryGetValue("repositories", out var repositoriesNode)) {
            if (repositoriesNode is TomlTableArray repositoriesList) {
                foreach (var repoNode in repositoriesList) {
                    if (repoNode is TomlTable repoTable) {
                        var setting = new Setting {
                            Name = repoTable["name"].ToString(),
                            Target = repoTable["target"].ToString(),
                            Display = repoTable["display"].ToString()
                        };
                        config.Repositories.Add(setting);
                    }
                }
            }
        }

        return config;
#endif
    }

    public static Task ForEach(this Config config, Func<(string url, string target, string display), Task> action) {
        if (!Directory.Exists(config.Destination)) {
            Directory.CreateDirectory(config.Destination);
        }

        var tasks = new Task[config.Repositories.Count];
        var index = 0;
        foreach (var setting in config.Repositories) {
            var url = $"{config.URL}{setting.Name}/releases/latest";

            var task = action.Invoke((url, setting.Target, setting.Display));
            tasks[index++] = task;
        }
        return Task.WhenAll(tasks);
    }
}

public class Config {
    public string URL { get; set; }

    public string Destination { get; set; }

    public List<Setting> Repositories { get; set; } = new List<Setting>();
}

public class Setting {
    public string Name { get; set; }

    public string Target { get; set; }

    public string Display { get; set; }
}