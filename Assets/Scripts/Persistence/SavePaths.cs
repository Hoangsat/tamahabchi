using System.IO;
using UnityEngine;

public static class SavePaths
{
    private const string SaveDirectoryName = "Saves";
    private const string SaveFileName = "savegame.json";
    private const string TempFileName = "savegame.tmp";
    private const string BackupFileName = "savegame.bak";

    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveDirectoryName);
    public static string MainSaveFilePath => Path.Combine(SaveDirectory, SaveFileName);
    public static string TempSaveFilePath => Path.Combine(SaveDirectory, TempFileName);
    public static string BackupSaveFilePath => Path.Combine(SaveDirectory, BackupFileName);

    public static void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }
    }
}
