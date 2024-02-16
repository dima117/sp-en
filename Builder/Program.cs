using CSharpMinifier;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Builder
{
    internal class Program
    {
        static Regex ImportRe = new Regex("// import:(?<path>.+)");

        class MyModule
        {
            public string filePath;
            public string content;
            public string[] deps;
        }

        static MyModule ReadModule(string filePath)
        {
            Console.WriteLine(filePath);
            var source = File.ReadAllLines(filePath);

            var copy = false;
            var deps = new HashSet<string>();
            var sb = new StringBuilder();

            foreach (var line in source)
            {
                if (Regex.IsMatch(line, "#region"))
                {
                    if (copy)
                    {
                        throw new Exception("error: nested region");
                    }

                    copy = true;
                }
                else if (Regex.IsMatch(line, "#endregion"))
                {
                    if (!copy)
                    {
                        throw new Exception("error: incorrect endregion");
                    }

                    copy = false;
                }
                else if (ImportRe.IsMatch(line))
                {
                    var res = ImportRe.Match(line);
                    var path = res.Groups["path"];
                    deps.Add(path.Value.Trim());

                }
                else if (copy)
                {
                    sb.AppendLine(line);
                }
            }

            var content = string.Join(string.Empty, Minifier.Minify(sb.ToString()));

            return new MyModule
            {
                filePath = filePath,
                content = content,
                deps = deps.ToArray()
            };
        }

        static string GetContent(string projectPath, string entryFileName)
        {
            var added = new HashSet<string>();
            var sb = new StringBuilder();

            sb.AppendLine($"//{DateTime.Now:G}");

            void AddModule(string basePath, string fileName)
            {
                var fullPath = Path.Combine(basePath, fileName);
                var module = ReadModule(fullPath);

                foreach (var dependencyRelativePath in module.deps)
                {
                    var dependencyFullPath = Path.GetFullPath(Path.Combine(basePath, dependencyRelativePath));

                    if (!added.Contains(dependencyFullPath))
                    {
                        var dependencyDirName = Path.GetDirectoryName(dependencyFullPath);
                        var dependencyFileName = Path.GetFileName(dependencyFullPath);

                        AddModule(dependencyDirName, dependencyFileName);
                    }
                }

                sb.Append(module.content);
                added.Add(fullPath);
            }

            AddModule(projectPath, entryFileName);

            return sb.ToString();
        }

        static void Main(string[] args)
        {
            var cwd = Directory.GetCurrentDirectory();
            var projectPath = Path.GetFullPath(Path.Combine(cwd, "..\\..\\..\\SpaceEngineers"));
            var builderPath = Path.GetFullPath(Path.Combine(cwd, "..\\..\\Dist"));

            var files = new string[] {
                "Scripts\\AimBot.cs",
                "Scripts\\Debug\\RotorTest.cs",
                "Scripts\\Fighter.cs",
                "Scripts\\Fortress.cs",
                "Scripts\\Spotter.cs",
                "Scripts\\TowShip.cs",
                "Scripts\\Printer.cs"
            };

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);

                var content = GetContent(projectPath, filePath);

                File.WriteAllText(Path.Combine(builderPath, fileName + ".txt"), content);
            }
        }
    }
}
