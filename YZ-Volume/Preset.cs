using System;
using System.Collections.Generic;

[Serializable]
public class MatrixControl
{
    public string Label { get; set; } = string.Empty;
    public List<string> CommandBases { get; set; } = new();

    // THE FINAL UPGRADE: A list to hold the gain for each CommandBase
    public List<double> InitialGains { get; set; } = new();
}

[Serializable]
public class Preset
{
    public string Name { get; set; } = string.Empty;
    public List<MatrixControl> Controls { get; set; } = new();
}