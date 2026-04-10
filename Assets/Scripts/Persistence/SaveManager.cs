using System;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private const string LegacySaveKey = "SAVE_DATA";
    private const string LegacyMigrationMarkerKey = "SAVE_DATA_MIGRATED_TO_FILE";

    public void Save(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        SavePaths.EnsureSaveDirectoryExists();

        SaveData normalized = SaveNormalizer.Normalize(data);
        string json = JsonUtility.ToJson(normalized, true);
        string tempPath = SavePaths.TempSaveFilePath;
        string mainPath = SavePaths.MainSaveFilePath;
        string backupPath = SavePaths.BackupSaveFilePath;

        File.WriteAllText(tempPath, json);

        if (File.Exists(mainPath))
        {
            File.Copy(mainPath, backupPath, true);
            File.Delete(mainPath);
        }

        File.Move(tempPath, mainPath);
        PlayerPrefs.SetInt(LegacyMigrationMarkerKey, 1);
        PlayerPrefs.Save();
    }

    public SaveData Load()
    {
        SaveData mainSave = TryLoadFromFile(SavePaths.MainSaveFilePath, "main save");
        if (mainSave != null)
        {
            return mainSave;
        }

        SaveData backupSave = TryLoadFromFile(SavePaths.BackupSaveFilePath, "backup save");
        if (backupSave != null)
        {
            Debug.LogWarning("Main save failed to load. Loaded backup save instead.");
            return backupSave;
        }

        SaveData legacySave = TryLoadLegacySave();
        if (legacySave != null)
        {
            Debug.Log("Migrated legacy PlayerPrefs save to file-based JSON save.");
            Save(legacySave);
            return legacySave;
        }

        bool anySaveArtifactExists =
            File.Exists(SavePaths.MainSaveFilePath) ||
            File.Exists(SavePaths.BackupSaveFilePath) ||
            PlayerPrefs.HasKey(LegacySaveKey);

        if (anySaveArtifactExists)
        {
            Debug.LogWarning("No valid save could be recovered. Creating a new default save.");
            return SaveNormalizer.CreateDefault();
        }

        return null;
    }

    public void Reset()
    {
        DeleteIfExists(SavePaths.TempSaveFilePath);
        DeleteIfExists(SavePaths.MainSaveFilePath);
        DeleteIfExists(SavePaths.BackupSaveFilePath);
        PlayerPrefs.DeleteKey(LegacySaveKey);
        PlayerPrefs.DeleteKey(LegacyMigrationMarkerKey);
        PlayerPrefs.Save();
    }

    private SaveData TryLoadFromFile(string path, string label)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"Ignoring empty {label} at {path}");
                return null;
            }

            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded == null)
            {
                Debug.LogWarning($"Ignoring unreadable {label} at {path}");
                return null;
            }

            return SaveNormalizer.Normalize(loaded);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load {label} at {path}: {ex.Message}");
            return null;
        }
    }

    private SaveData TryLoadLegacySave()
    {
        if (!PlayerPrefs.HasKey(LegacySaveKey))
        {
            return null;
        }

        try
        {
            string json = PlayerPrefs.GetString(LegacySaveKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            return loaded != null ? SaveNormalizer.Normalize(loaded) : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load legacy PlayerPrefs save: {ex.Message}");
            return null;
        }
    }

    private void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
