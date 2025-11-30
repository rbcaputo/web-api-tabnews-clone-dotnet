using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

string exePath = AppContext.BaseDirectory;
string repoRoot = Directory.GetParent(exePath)?
  .Parent?
  .Parent?
  .Parent?
  .Parent?
  .Parent?
  .FullName ?? Directory.GetCurrentDirectory();

// Print the repo root to validate.
Console.WriteLine($"Repo root: {repoRoot}");

string scriptsPath = Path.Combine(repoRoot, "scripts.jsonc");

if (!File.Exists(scriptsPath))
{
  Console.Error.WriteLine($"Could not find scripts.jsonc at: {scriptsPath}.");
  Environment.Exit(2);
}

var options = new JsonSerializerOptions
{
  ReadCommentHandling = JsonCommentHandling.Skip,
  AllowTrailingCommas = true
};

var scripts = JsonSerializer.Deserialize<Dictionary<string, string>>(
  File.ReadAllText(scriptsPath), options
) ?? new Dictionary<string, string>();

if (args.Length == 0)
{
  Console.WriteLine("Available scripts:");
  foreach (var key in scripts.Keys) Console.WriteLine($"  {key}");
  return;
}

string scriptName = args[0];
if (!scripts.TryGetValue(scriptName, out var command))
{
  Console.Error.WriteLine($"Script '{scriptName}' not found.");
  Console.WriteLine("Available scripts:");
  foreach (var key in scripts.Keys) Console.WriteLine($"  {key}");
  Environment.Exit(3);
}

// Append extra args (args[1..]) safely.
if (args.Length > 1)
{
  string[] extras = args[1..];
  string Quote(string arg) =>
    arg.Contains(' ') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg;

  command += " " + string.Join(' ', extras.Select(Quote));
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
  WorkingDirectory = repoRoot,
  UseShellExecute = false
};
psi.ArgumentList.Add(shellArg);
psi.ArgumentList.Add(command);

var process = Process.Start(psi);
if (process == null)
{
  Console.Error.WriteLine("Failed to start process.");
  Environment.Exit(4);
}

process.WaitForExit();
Environment.Exit(process.ExitCode);
