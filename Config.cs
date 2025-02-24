using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace LspManager;

public static class ConfigHelper {
    static string GetTomlStringValue(DocumentSyntax document, string key)
    {
        if (document.ToModel().TryGetValue(key, out var value))
        {
            return value.ToString();
        }
        return null;
    }

    public static async Task<Config> Load(string path) {
        var text = await File.ReadAllTextAsync(path);
        var document = Toml.Parse(text);

        var config = new Config();

        // Access simple properties directly
        config.URL = GetTomlStringValue(document, "url");
        config.Destination = GetTomlStringValue(document, "destination");

        if (document.ToModel().TryGetValue("repositories", out var repositoriesNode))
        {
            var repositoriesList = repositoriesNode as TomlTableArray;
            Console.WriteLine(repositoriesList == null);
            foreach (var repoNode in repositoriesList)
            {
                if (repoNode is TomlTable repoTable)
                {
                    var setting = new Setting
                    {
                        Name = repoTable["name"].ToString(),
                        Target = repoTable["target"].ToString()
                    };
                    config.Repositories.Add(setting);
                }
            }
        }

        return config;
        // return Toml.Parse(text).ToModel<Config>();
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

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Config {
    [DataMember(Name = "url")]
    public string URL { get; set; }

    [DataMember(Name = "destination")]
    public string Destination { get; set; }

    [DataMember(Name = "repositories")]
    public List<Setting> Repositories { get; set; } = new List<Setting>();
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Setting {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "target")]
    public string Target { get; set; }
}