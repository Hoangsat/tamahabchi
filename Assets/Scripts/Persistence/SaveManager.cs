using UnityEngine;

public class SaveManager
{
    private const string SAVE_KEY = "SAVE_DATA";

    public void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public SaveData Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            return null;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public void Reset()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
    }
}
