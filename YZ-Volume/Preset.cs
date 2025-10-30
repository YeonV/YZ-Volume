using System.Collections.Generic;

// This class defines a single user-configurable Matrix slider.
public class MatrixControl
{
    public string Label { get; set; } = string.Empty;
    public string CommandBase { get; set; } = string.Empty;
    public double InitialGain { get; set; }
}

// This class defines a complete preset, including its name and the list
// of Matrix controls it uses.
public class Preset
{
    public string Name { get; set; } = string.Empty;
    public List<MatrixControl> Controls { get; set; } = new();
}