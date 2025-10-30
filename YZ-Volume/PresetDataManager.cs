using System.Collections.Generic;

public static class PresetDataManager
{
    // This static property holds the single source of truth for all preset data.
    public static List<Preset> Presets { get; } = new()
    {
        // --- Preset 1: PC 5.1 ---
        new Preset
        {
            Name = "PC 5.1",
            Controls = new List<MatrixControl>
            {
                new MatrixControl { Label = "FL", CommandBase = "Point(VAIO2.IN[1],WIN1.OUT[1])", InitialGain = -10.0 },
                new MatrixControl { Label = "FR", CommandBase = "Point(VAIO2.IN[2],WIN1.OUT[2])", InitialGain = -9.0 },
                new MatrixControl { Label = "C",  CommandBase = "Point(VAIO2.IN[3],WIN3.OUT[1])", InitialGain = -6.0 },
                new MatrixControl { Label = "S",  CommandBase = "Point(VAIO2.IN[3],WIN3.OUT[2])", InitialGain = -4.5 },
                new MatrixControl { Label = "RL", CommandBase = "Point(VAIO2.IN[5],WIN4.OUT[1])", InitialGain = 0.0 },
                new MatrixControl { Label = "RR", CommandBase = "Point(VAIO2.IN[6],WIN4.OUT[2])", InitialGain = -1.0 }
            }
        },
        // --- Preset 2: PC 2.0 ---
        new Preset
        {
            Name = "PC 2.0",
            Controls = new List<MatrixControl>
            {
                // This preset creates a "dual mono" or stereo mixdown to the main speakers.
                new MatrixControl { Label = "FL", CommandBase = "Point(VAIO2.IN[1],WIN1.OUT[1])", InitialGain = 0.0 },
                new MatrixControl { Label = "FR", CommandBase = "Point(VAIO2.IN[2],WIN1.OUT[2])", InitialGain = 0.0 },
                new MatrixControl { Label = "C",  CommandBase = "Point(VAIO2.IN[3],WIN1.OUT[1])", InitialGain = 0.0 },
                new MatrixControl { Label = "S",  CommandBase = "Point(VAIO2.IN[3],WIN1.OUT[2])", InitialGain = 0.0 },
                new MatrixControl { Label = "RL", CommandBase = "Point(VAIO2.IN[5],WIN1.OUT[1])", InitialGain = 0.0 },
                new MatrixControl { Label = "RR", CommandBase = "Point(VAIO2.IN[6],WIN1.OUT[2])", InitialGain = 0.0 }
            }
        },
        // --- Preset 3: Beamer 5.1 ---
        new Preset
        {
            Name = "Beamer 5.1",
            Controls = new List<MatrixControl>
            {
                new MatrixControl { Label = "FL", CommandBase = "Point(VAIO2.IN[1],WIN4.OUT[2])", InitialGain = -1.0 },
                new MatrixControl { Label = "FR", CommandBase = "Point(VAIO2.IN[2],WIN4.OUT[1])", InitialGain = 0.0 },
                new MatrixControl { Label = "C",  CommandBase = "Point(VAIO2.IN[3],WIN3.OUT[1])", InitialGain = -6.0 },
                new MatrixControl { Label = "S",  CommandBase = "Point(VAIO2.IN[3],WIN3.OUT[2])", InitialGain = -4.5 },
                new MatrixControl { Label = "RL", CommandBase = "Point(VAIO2.IN[5],WIN1.OUT[2])", InitialGain = -9.0 },
                new MatrixControl { Label = "RR", CommandBase = "Point(VAIO2.IN[6],WIN1.OUT[1])", InitialGain = -10.0 }
            }
        }
    };
}