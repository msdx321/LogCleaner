using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace LogCleaner.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly FileDialogManager fileDialogManager;
    private readonly Plugin plugin;
    private DirectoryInfo di;
    private FileInfo[] fiArray;

    public MainWindow(Plugin plugin) : base(
        "Log Status", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
        fileDialogManager = plugin.FileDialogManager;

        this.plugin = plugin;
        try
        {
            di = new DirectoryInfo(configuration.LogPath);
        }
        catch (Exception)
        {
            configuration.LogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            configuration.Save();

            di = new DirectoryInfo(configuration.LogPath);
        }

        fiArray = di.GetFiles();

        if (configuration.AutoCompress) FileUtils.AutoCompress(di, configuration.CompressThreshold);

        if (configuration.AutoClean) FileUtils.AutoClean(di, configuration.CleanThreshold);
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (FileUtils.NeedRefresh())
        {
            di.Refresh();
            fiArray = di.GetFiles();
        }

        if (ImGui.BeginTable("Logs", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable,
                             new Vector2(ImGui.GetWindowWidth() - 50, 500)))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Size");
            ImGui.TableHeadersRow();

            var i = 0;
            foreach (var fi in fiArray)
            {
                if (!(fi.FullName.EndsWith(".log") || fi.FullName.EndsWith(".zst")))
                    continue;

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text(fi.Name);

                ImGui.TableSetColumnIndex(1);
                ImGui.Text(SizeConvert(fi.Length));

                ImGui.TableSetColumnIndex(2);
                if (FileUtils.IsFileLocked(fi.FullName)) continue;

                if (fi.FullName.EndsWith(".zst"))
                {
                    if (!FileUtils.IsWorking())
                    {
                        ImGui.PushID(i);
                        if (ImGui.Button("Decompress")) FileUtils.Decompress(fi.FullName);
                        ImGui.PopID();
                    }
                }
                else
                {
                    if (!FileUtils.IsWorking())
                    {
                        ImGui.PushID(i);
                        if (ImGui.Button("Compress")) FileUtils.Compress(fi.FullName);
                        ImGui.PopID();
                    }
                }

                i++;
            }

            ImGui.EndTable();
        }

        if (ImGui.Button("Compress All")) FileUtils.Compress(fiArray);
        ImGui.SameLine();
        if (ImGui.Button("Decompress All")) FileUtils.Decompress(fiArray);
        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
            di.Refresh();
            fiArray = di.GetFiles();
        }

        ImGui.Text($"Current logs path: {plugin.Configuration.LogPath}");
        ImGui.SameLine();
        if (ImGui.Button("Set logs path"))
        {
            fileDialogManager.OpenFolderDialog("Pick a folder to save logs to", (success, path) =>
            {
                if (!success) return;
                configuration.LogPath = path;
                di = new DirectoryInfo(path);
            }, configuration.LogPath);
        }

        ImGui.Text("Clean logs older than");
        ImGui.SameLine();
        var cleanThreshold = configuration.CleanThreshold;
        ImGui.PushItemWidth(150);
        ImGui.InputInt("Days##Clean", ref cleanThreshold);
        ImGui.PopItemWidth();
        configuration.CleanThreshold = cleanThreshold;
        ImGui.SameLine();
        if (ImGui.Button("Clean"))
            FileUtils.AutoClean(di, configuration.CleanThreshold);

        ImGui.SameLine();
        var autoClean = configuration.AutoClean;
        ImGui.Checkbox("Auto Clean", ref autoClean);
        configuration.AutoClean = autoClean;

        ImGui.Text("Compress logs older than");
        ImGui.SameLine();
        var compressThreshold = configuration.CompressThreshold;
        ImGui.PushItemWidth(150);
        ImGui.InputInt("Days##Compress", ref compressThreshold);
        ImGui.PopItemWidth();
        configuration.CompressThreshold = compressThreshold;
        ImGui.SameLine();
        if (ImGui.Button("Compress"))
            FileUtils.AutoCompress(di, configuration.CompressThreshold);

        ImGui.SameLine();
        var autoCompress = configuration.AutoCompress;
        ImGui.Checkbox("Auto Compress", ref autoCompress);
        configuration.AutoCompress = autoCompress;

        configuration.Save();
    }

    private static string SizeConvert(long length)
    {
        const long byteConversion = 1000;
        string[] sizeSuffixes = { "Bytes", "KB", "MB", "GB", "TB" };
        switch (length)
        {
            case < 0:
                return "-" + SizeConvert(-length);
            case 0:
                return "0.0 Bytes";
        }

        var mag = (int)Math.Log(length, byteConversion);
        var adjustedSize = length / Math.Pow(byteConversion, mag);

        return $"{adjustedSize:n2} {sizeSuffixes[mag]}";
    }
}
