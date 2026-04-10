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
    private static int currentRunPeakCoins;
    private static int currentRunWave;
    private static int currentRunBotsKilled;

    // Inicia ejecución.
    public static void BeginRun()
    {
        currentRunCoins = 0;
        currentRunPeakCoins = 0;
        currentRunWave = 0;
        currentRunBotsKilled = 0;
    }

    // Actualiza monedas.
    public static void UpdateCoins(int currentCoins)
    {
        currentRunCoins = Mathf.Max(0, currentCoins);
        currentRunPeakCoins = Mathf.Max(currentRunPeakCoins, currentRunCoins);

        if (currentRunPeakCoins <= GetMaxCoins())
        {
            return;
        }

        PlayerPrefs.SetInt(MaxCoinsKey, currentRunPeakCoins);
        PlayerPrefs.Save();
    }

    // Actualiza oleada.
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

    // Gestiona register bot kill.
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

    // Gestiona commit ultimo ejecución.
    public static void CommitLastRun()
    {
        PlayerPrefs.SetInt(LastCoinsKey, Mathf.Max(0, currentRunPeakCoins));
        PlayerPrefs.SetInt(LastWaveKey, Mathf.Max(0, currentRunWave));
        PlayerPrefs.SetInt(LastBotsKilledKey, Mathf.Max(0, currentRunBotsKilled));
        PlayerPrefs.Save();
    }

    // Obtiene ultimo ejecución estadísticas.
    public static void GetLastRunStats(out int coins, out int wave, out int bots)
    {
        coins = Mathf.Max(0, PlayerPrefs.GetInt(LastCoinsKey, 0));
        wave = Mathf.Max(0, PlayerPrefs.GetInt(LastWaveKey, 0));
        bots = Mathf.Max(0, PlayerPrefs.GetInt(LastBotsKilledKey, 0));
    }

    // Obtiene best estadísticas.
    public static void GetBestStats(out int maxCoins, out int maxWave, out int maxBotsKilled)
    {
        maxCoins = GetMaxCoins();
        maxWave = GetMaxWave();
        maxBotsKilled = GetMaxBotsKilled();
    }

    // Obtiene maximo monedas.
    private static int GetMaxCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MaxCoinsKey, 0));
    }

    // Obtiene maximo oleada.
    private static int GetMaxWave()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MaxWaveKey, 0));
    }

    // Obtiene maximo bots killed.
    private static int GetMaxBotsKilled()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(MaxBotsKilledKey, 0));
    }
}
