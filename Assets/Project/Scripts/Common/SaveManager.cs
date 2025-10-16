using UnityEngine;

public static class SaveManager
{
    public static int highScore;

    public static void Save()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }
}
