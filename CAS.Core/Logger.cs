using System.IO;
using System.Diagnostics;

namespace CAS.Core
{
  public static class Logger
  {
    public static readonly string logFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CAS", "log.txt");

    public static void Log(string message)
    {
      try
      {
        Debug.WriteLine($"INFO-{DateTime.Now}: {message}{Environment.NewLine}");
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
      }
      catch { /* Ignore logging failures */ }
    }

    public static void LogWarn(string message)
    {
      try
      {
        Debug.WriteLine($"INFO-{DateTime.Now}: {message}{Environment.NewLine}");
        File.AppendAllText(logFilePath, $"WARN-{DateTime.Now}: {message}{Environment.NewLine}");
      }
      catch { /* Ignore logging failures */ }
    }

    public static void LogError(string message)
    {
      try
      {
        Debug.WriteLine($"INFO-{DateTime.Now}: {message}{Environment.NewLine}");
        File.AppendAllText(logFilePath, $"ERROR-{DateTime.Now}: {message}{Environment.NewLine}");
      }
      catch { /* Ignore logging failures */ }
    }

    public static void LogInfo(string message)
    {
      try
      {
        Debug.WriteLine($"INFO-{DateTime.Now}: {message}{Environment.NewLine}");
        File.AppendAllText(logFilePath, $"INFO-{DateTime.Now}: {message}{Environment.NewLine}");
      }
      catch { /* Ignore logging failures */ }
    }
  }
}
