using UnityEngine;

public static class RunStatsStore
{
    private const string MaxCoinsKey = "score.maxCoins";
    private const string MaxWaveKey = "score.maxWave";
    private const string MaxBotsKilledKey = "score.maxBotsKilled";

    private static int currentRunBotsKilled;

    public static void BeginRun()
    {
        currentRunBotsKilled = 0;
    }

    public static void UpdateCoins(int currentCoins)
    {
        if (currentCoins <= GetMaxCoins())
        {
            return;
        }

        PlayerPrefs.SetInt(MaxCoinsKey, Mathf.Max(0, currentCoins));
        PlayerPrefs.Save();
    }

    public static void UpdateWave(int waveIndex)
    {
        if (waveIndex <= GetMaxWave())
        {
            return;
        }

        PlayerPrefs.SetInt(MaxWaveKey, Mathf.Max(0, waveIndex));
        PlayerPrefs.Save();
    }

    public static void RegisterBotKill()
    {
        currentRunBotsKilled++;

        if (currentRunBotsKilled <= GetMaxBotsKilled())
        {
            return;
        }

        PlayerPrefs.SetInt(MaxBotsKilledKey, currentRunBotsKilled);
        PlayerPrefs.Save();
    }

    public static void GetBestStats(out int maxCoins, out int maxWave, out int maxBotsKilled)
    {
        maxCoins = GetMaxCoins();
        maxWave = GetMaxWave();
        maxBotsKilled = GetMaxBotsKilled();
    }

    private static int GetMaxCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MaxCoinsKey, 0));
    }

    private static int GetMaxWave()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MaxWaveKey, 0));
    }

    private static int GetMaxBotsKilled()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MaxBotsKilledKey, 0));
    }
}
