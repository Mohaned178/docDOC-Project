using System;
using System.IO;
using System.Linq;

var rootDir = @"f:\docDOC\src";
var testDir = @"f:\docDOC\tests";

void ProcessDirectory(string dir)
{
    if (!Directory.Exists(dir)) return;
    foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
    {
        var content = File.ReadAllText(file);
        var original = content;
        
        content = content.Replace("Guid.NewGuid().ToString()", "DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()");
        content = content.Replace("Guid.NewGuid()", "0");
        content = content.Replace("Guid.Empty", "0");
        content = content.Replace("Guid.Parse", "int.Parse");
        content = content.Replace("Guid.TryParse", "int.TryParse");
        content = content.Replace("Guid?", "int?");
        content = content.Replace(" Guid ", " int ");
        content = content.Replace("(Guid ", "(int ");
        content = content.Replace("<Guid>", "<int>");
        content = content.Replace(" IEnumerable<Guid> ", " IEnumerable<int> ");
        content = content.Replace("{Guid:", "{int:");
        content = content.Replace(" Guid,", " int,");
        
        if (content != original)
        {
            File.WriteAllText(file, content);
            Console.WriteLine($"Updated: {file}");
        }
    }
}

ProcessDirectory(rootDir);
ProcessDirectory(testDir);
Console.WriteLine("Done.");
