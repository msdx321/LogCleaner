using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Dalamud.Logging;
using Microsoft.VisualBasic.FileIO;
using ZstdSharp;

namespace LogCleaner;

public static class FileUtils
{
    private static ulong CurWorkers;

    public static bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }

    public static bool IsWorking()
    {
        return Interlocked.Read(ref CurWorkers) > 0;
    }

    public static void Compress(string filePath)
    {
        var compressPath = filePath + ".zst";

        if (!File.Exists(filePath))
        {
            PluginLog.Debug("LogCleaner: Error: {0} doesn't exist.", filePath);
            return;
        }

        if (File.Exists(compressPath))
        {
            PluginLog.Error("LogCleaner: Error: Output {0} file exists.", compressPath);
            return;
        }

        if (IsFileLocked(filePath))
        {
            PluginLog.Error("LogCleaner: Error: {0} is locked.", filePath);
            return;
        }

        if (!filePath.EndsWith(".log"))
        {
            PluginLog.Error("LogCleaner: Error: {0} is not a log file.", filePath);
            return;
        }

        new Thread(() =>
        {
            Interlocked.Increment(ref CurWorkers);
            try
            {
                var src = File.ReadAllBytes(filePath);
                using var compressor = new Compressor(1);
                var compressed = compressor.Wrap(src);
                var output = File.Open(filePath, FileMode.Truncate);
                output.Write(compressed);
                output.Dispose();
                File.Move(filePath, compressPath);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"LogCleaner: Error: {ex.Message}\n{ex.StackTrace ?? ""}");
            }

            Interlocked.Decrement(ref CurWorkers);
        }).Start();
    }

    public static void Compress(List<string> filePaths)
    {
        new Thread(() =>
        {
            foreach (var filePath in filePaths) Compress(filePath);
        }).Start();
    }

    public static void Compress(FileInfo[] fiArray)
    {
        new Thread(() =>
        {
            var fullNames = new List<string>();

            foreach (var fi in fiArray)
                if (fi.FullName.EndsWith(".log") && !IsFileLocked(fi.FullName))
                    fullNames.Add(fi.FullName);

            Compress(fullNames);
        }).Start();
    }

    public static void Decompress(string filePath)
    {
        var decompressPath = filePath.Replace(".zst", "");

        if (!File.Exists(filePath))
        {
            PluginLog.Error("LogCleaner: Error: {0} doesn't exist.", filePath);
            return;
        }

        if (File.Exists(decompressPath))
        {
            PluginLog.Error("LogCleaner: Error: Output {0} file exists.", decompressPath);
            return;
        }

        if (IsFileLocked(filePath))
        {
            PluginLog.Error("LogCleaner: Error: {0} is locked.", filePath);
            return;
        }

        if (!filePath.EndsWith(".zst"))
        {
            PluginLog.Error("LogCleaner: Error: {0} is not an archive.", filePath);
            return;
        }

        new Thread(() =>
        {
            Interlocked.Increment(ref CurWorkers);
            try
            {
                var buffer = File.ReadAllBytes(filePath);
                using var decompressor = new Decompressor();
                var decompressed = decompressor.Unwrap(buffer);
                var f = File.Open(filePath, FileMode.Truncate, FileAccess.ReadWrite, FileShare.None);
                f.Write(decompressed);
                f.Dispose();
                File.Move(filePath, decompressPath);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Log Cleaner: Error: {ex.Message}\n{ex.StackTrace ?? ""}");
            }

            Interlocked.Decrement(ref CurWorkers);
        }).Start();
    }

    public static void Decompress(List<string> filePaths)
    {
        new Thread(() =>
        {
            foreach (var filePath in filePaths) Decompress(filePath);
        }).Start();
    }

    public static void Decompress(FileInfo[] fiArray)
    {
        new Thread(() =>
        {
            var fullNames = new List<string>();

            foreach (var fi in fiArray)
                if (fi.FullName.EndsWith(".zst") && !IsFileLocked(fi.FullName))
                    fullNames.Add(fi.FullName);

            Decompress(fullNames);
        }).Start();
    }

    public static int CheckAge(FileInfo fi)
    {
        DateTime date;
        try
        {
            date = DateTime.ParseExact(fi.Name.Split(".")[0].Split("_")[2], "yyyyMMdd",
                                       CultureInfo.InvariantCulture);
        }
        catch
        {
            date = File.GetCreationTime(fi.FullName);
        }

        return (DateTime.Now - date).Days;
    }

    public static void AutoCompress(DirectoryInfo di, int days)
    {
        di.Refresh();
        var fiArray = di.GetFiles();
        var fullNames = new List<string>();

        new Thread(() =>
        {
            foreach (var fi in fiArray)
            {
                if (CheckAge(fi) <= days)
                    continue;

                if (fi.FullName.EndsWith(".log") && !IsFileLocked(fi.FullName))
                    fullNames.Add(fi.FullName);
            }

            Compress(fullNames);
        }).Start();
    }

    public static void AutoClean(DirectoryInfo di, int days)
    {
        di.Refresh();
        var fiArray = di.GetFiles();
        var fullNames = new List<string>();

        new Thread(() =>
        {
            foreach (var fi in fiArray)
            {
                if (CheckAge(fi) <= days)
                    continue;

                if (!IsFileLocked(fi.FullName))
                    fullNames.Add(fi.FullName);
            }

            foreach (var f in fullNames)
                FileSystem.DeleteFile(f, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }).Start();
    }
}
