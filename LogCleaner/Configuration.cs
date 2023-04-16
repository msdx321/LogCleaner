using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace LogCleaner;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public string LogPath { get; set; } = "";
    public int CleanThreshold { get; set; } = 7;
    public bool AutoClean { get; set; }
    public int CompressThreshold { get; set; } = 5;
    public bool AutoCompress { get; set; }
    public int Version { get; set; } = 0;

    public void Initialize(DalamudPluginInterface pi)
    {
        this.pluginInterface = pi;
    }

    public void Save()
    {
        pluginInterface!.SavePluginConfig(this);
    }
}
