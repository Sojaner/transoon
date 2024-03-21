## ***Tran-sooner*** (**sooner** rather than ~~later~~), for C# source code documentation, comments, and strings
**TranSooner** is a **.NET Global Tool** for translating all the ***documentation***, ***comments***, and ***strings*** in a **C#** codebase.
- This tool was originally created for translating the codebase of **DotnetSpider** library from [Chinese](https://github.com/dotnetcore/DotnetSpider) to [English](https://github.com/Sojaner/DotnetSpider/tree/en-updated).

### Installation
```bash
dotnet tool install -g transooner
```
- This tool relies on .NET 8.0

### Usage example
```bash
transooner -d /Users/sojaner/Git/GitHub/DotnetSpider -k AIzaSyD-7kgBVqzyJb0e6k3yvh1PEw0F42xS4T8 -t google -s Debug #The API key here is a dummy API key
```
- **Important note:** Using ***Google Translate*** without and API Key will switch to ***Google's free Translation API** that is provided only for ***demo purposes*** and should not be used in commercial and production environments.

### Usage options
| Option                        | Description                                                                                                  | Default                    |
|-------------------------------|--------------------------------------------------------------------------------------------------------------|----------------------------|
| -d, --directory     (optional)| Directory containing .cs files.                                                                              | Current working directory  |
| -k, --api-key       (optional)| Translate API Key.                                                                                           |                            |
| -t, --translator    (optional)| Translator to use (google, deepl).                                                                           | google                     |
| -l, --language      (optional)| Language to translate to.                                                                                    | en-US                      |
| -r, --regex         (optional)| Regex pattern to match comments to be translated.                                                            | [^\x00-\x7F]               |
| -i, --includes      (optional)| Glob pattern to include files.                                                                               | **/*.cs                    |
| -e, --excludes      (optional)| Glob pattern to exclude files.                                                                               | bin/*.* obj/*.*            |
| -c, --capitalize    (optional)| Capitalize first letter of translated comments.                                                              | true                       |
| -y, --yes           (optional)| Acknowledge and disable the "Google's free Translation API" usage warning by implicitly answering "yes".     | false                      |
| -s, --symbols       (optional)| Preprocessor symbols to use with the C# parser for translating conditional code parts.                       | no symbols                 |
| -n, --no-logo       (optional)| Skip printing the logo.                                                                                      | false                      |
| --no-comments       (optional)| Skip translating comments.                                                                                   | false                      |
| --no-strings        (optional)| Skip translating strings.                                                                                    | false                      |
| --no-documentation  (optional)| Skip translating documentation comments.                                                                     | false                      |
| --no-progress       (optional)| Skip printing progress.                                                                                      | false                      |
| --help              (optional)| Display this help screen.                                                                                    |                            |
| --version           (optional)| Display version information.                                                                                 |                            |
