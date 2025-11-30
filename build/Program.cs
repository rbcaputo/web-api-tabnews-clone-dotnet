using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

string repoBase = AppContext.BaseDirectory;
string scriptsPath = Path.Combine(Path.GetDirectoryName(repoBase) ?? ".", "scripts.json");

// Fallback if running with dotnet run where BaseDirectory points into bin; try the project folder.
if (!File.Exists(scriptsPath))
{
  var alt = Path.Combine(Directory.GetCurrentDirectory(), "build", "scripts.json");
  if (File.Exists(alt)) scriptsPath = alt;
}

if (!File.Exists(scriptsPath))
{
  Console.Error.WriteLine($"Could not find scripts.json at: {scriptsPath}");
  Environment.Exit(2);
}

var scripts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(scriptsPath))
  ?? new Dictionary<string, string>();

if (args.Length == 0)
{
  Console.WriteLine("Available scripts:");
  foreach (var key in scripts.Keys) Console.WriteLine($"  {key}");
  return;
}

var scriptName = args[0];
if (!scripts.TryGetValue(scriptName, out var command))
{
  Console.Error.WriteLine($"Script '{scriptName}' not found. Available scripts:");
  foreach (var key in scripts.Keys) Console.WriteLine($"  {key}");
  Environment.Exit(3);
}

// Append extra args (args[1..]) safely.
if (args.Length > 1)
{
  string[] extras = args[1..];
  string JoinArg(string arg) => arg.Contains(' ') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg;
  command += " " + string.Join(' ', extras.Select(JoinArg));
}

string shell, shellArg;
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
  shell = "cmd.exe";
  shellArg = "/c";
}
else
{
  shell = "/bin/bash";
  shellArg = "-c";
}

var psi = new ProcessStartInfo
{
  FileName = shell,
  ArgumentList = { shellArg, command },
  UseShellExecute = false,
  RedirectStandardInput = false,
  RedirectStandardOutput = false,
  RedirectStandardError = false
};

var process = Process.Start(psi)!;
process.WaitForExit();
Environment.Exit(process.ExitCode);
