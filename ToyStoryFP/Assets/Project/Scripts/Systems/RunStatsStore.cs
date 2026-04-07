using UnityEngine;

public static class RunStatsStore
{
    private const string MaxCoinsKey = "score.maxCoins";
    private const string MaxWaveKey = "score.maxWave";
    private const string MaxBotsKilledKey = "score.maxBotsKilled";
    private const string LastCoinsKey = "score.lastCoins";
    private const string LastWaveKey = "score.lastWave";
    private const string LastBotsKilledKey = "score.lastBotsKilled";

    private static int currentRunCoins;
    private static int currentRunWave;
    private static int currentRunBotsKilled;

    public static void BeginRun()
    {
        currentRunCoins = 0;
        currentRunWave = 0;
        currentRunBotsKilled = 0;
    }

    public static void UpdateCoins(int currentCoins)
    {
        currentRunCoins = Mathf.Max(0, currentCoins);

        if (currentCoins <= GetMaxCoins())
        {
            return;
        }

        PlayerPrefs.SetInt(MaxCoinsKey, currentRunCoins);
        PlayerPrefs.Save();
    }

    public static void UpdateWave(int waveIndex)
    {
        currentRunWave = Mathf.Max(0, waveIndex);

        if (waveIndex <= GetMaxWave())
        {
            return;
        }

        PlayerPrefs.SetInt(MaxWaveKey, currentRunWave);
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

    public static void CommitLastRun()
    {
        PlayerPrefs.SetInt(LastCoinsKey, Mathf.Max(0, currentRunCoins));
        PlayerPrefs.SetInt(LastWaveKey, Mathf.Max(0, currentRunWave));
        PlayerPrefs.SetInt(LastBotsKilledKey, Mathf.Max(0, currentRunBotsKilled));
        PlayerPrefs.Save();
    }

    public static void GetLastRunStats(out int coins, out int wave, out int bots)
    {
        coins = Mathf.Max(0, PlayerPrefs.GetInt(LastCoinsKey, 0));
        wave = Mathf.Max(0, PlayerPrefs.GetInt(LastWaveKey, 0));
        bots = Mathf.Max(0, PlayerPrefs.GetInt(LastBotsKilledKey, 0));
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
