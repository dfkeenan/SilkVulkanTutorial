
using System.IO;
using System.Diagnostics;

var currentChapter = Directory.EnumerateDirectories(Directory.GetCurrentDirectory()).OrderByDescending(d => d).First();
currentChapter = Path.GetFileName(currentChapter);
var number = currentChapter.Split('_')[0];
var nextNumber = int.Parse(number) + 1;
var nextChapter = $"{nextNumber}_{Args[0]}";

Console.WriteLine($"Creating chapter {nextChapter}");

Directory.CreateDirectory(nextChapter);

var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)  
{  
    ".cs", ".csproj", ".frag", ".vert" 
};

var files = Directory.EnumerateFiles(currentChapter, "*.*", SearchOption.TopDirectoryOnly)
    .Where(s => extensions.Contains(Path.GetExtension(s)));

string nextProjectName = null;

foreach(var file in files)
{
    var destinationFile = Path.Combine(nextChapter, Path.GetFileName(file));

    if(string.Equals(Path.GetExtension(file), ".csproj",StringComparison.OrdinalIgnoreCase))
    {
        destinationFile = Path.Combine(nextChapter, $"{nextChapter}.csproj");
        nextProjectName = destinationFile;
    }

    Console.WriteLine($"Copying file {file} --> {destinationFile}");

    File.Copy(file, destinationFile);
}

if(nextProjectName is {})
{
    Process.Start("dotnet", $"sln add \"{nextProjectName}\"");
}