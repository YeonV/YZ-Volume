using System;
using System.Collections.Generic;

[Serializable]
public class MatrixControl
{
    public string Label { get; set; } = string.Empty;
    public List<string> CommandBases { get; set; } = new();
    public List<double> InitialGains { get; set; } = new();
}

[Serializable]
public class PresetZoneData
{
    public string? Slotin0 { get; set; } // and all other string properties...
}

[Serializable]
public class Preset
{
    public string Name { get; set; } = string.Empty;
    public List<MatrixControl> Controls { get; set; } = new();
    public PresetZoneData? ZoneData { get; set; }

    // THE FINAL PROPERTY
    public int VbanIndex { get; set; } = 1; // Default to 1
}