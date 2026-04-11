using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class WaveManagerPlayModeTests
{
    [UnityTest]
    public IEnumerator WaveManager_CanEnterIntermission_AndRestartWave()
    {
        GameObject waveManagerObject = new GameObject("WaveManagerTest");
        GameObject waveSpawnerObject = new GameObject("WaveSpawnerTest");
        WaveManager waveManager = waveManagerObject.AddComponent<WaveManager>();
        WaveSpawner waveSpawner = waveSpawnerObject.AddComponent<WaveSpawner>();
        waveManager.enabled = false;

        bool waveStarted = false;
        int startedWaveIndex = -1;
        waveManager.WaveStarted += waveIndex =>
        {
            waveStarted = true;
            startedWaveIndex = waveIndex;
        };

        SetPrivateField(waveManager, "waveSpawner", waveSpawner);

        InvokePrivateMethod(waveManager, "StartNextWave");
        yield return null;

        Assert.That(waveStarted, Is.True);
        Assert.That(startedWaveIndex, Is.EqualTo(1));
        Assert.That(waveManager.CurrentState, Is.EqualTo(WaveManager.WaveRuntimeState.WaveInProgress));

        InvokePrivateMethod(waveManager, "BeginIntermission");
        yield return null;

        Assert.That(waveManager.CurrentState, Is.EqualTo(WaveManager.WaveRuntimeState.Intermission));

        InvokePrivateMethod(waveManager, "StartNextWave");
        yield return null;

        Assert.That(waveManager.CurrentState, Is.EqualTo(WaveManager.WaveRuntimeState.WaveInProgress));
        Assert.That(waveManager.CurrentWaveIndex, Is.EqualTo(2));

        Object.Destroy(waveManagerObject);
        Object.Destroy(waveSpawnerObject);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"No se encontro el campo privado '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"No se encontro el metodo privado '{methodName}'.");
        method.Invoke(target, null);
    }
}
