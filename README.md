# BinGet

Pulls the latest binaries and zips from public github repositories. This utility reads from 
a `config.toml` that you supply.

## Build instructions
Run the following in your terminal. Requires **.NET 8.0** or higher.
```
dotnet publish -c Release
```

## Example config
List the repositories with a display name.

```toml
url = "https://api.github.com/repos/"
destination = "your-destination-path"

[[repositories]]
name = "OmniSharp/omnisharp-roslyn"
target = "omnisharp-win-x64.zip"
display = "omnisharp"

[[repositories]]
name = "LuaLS/lua-language-server"
target = "win32-x64.zip"
display = "lua"

[[repositories]]
name = "shader-slang/slang"
target = "windows-x86_64.zip"
display = "slang"
```

* **name**: The github organization or user with the repo's name
* **target**: The build release naming convention, e.g OmniSharp is targetting win-x64 in this example
* **display**: The name of the directory to extract to defined at the `destination`

For example if the `destination` is `C:\dev` then omnisharp will extract to `C:\dev\omnisharp`.

## Running it
```bash
binget.exe config.toml
```
