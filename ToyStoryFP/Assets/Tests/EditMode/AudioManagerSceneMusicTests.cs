using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class AudioManagerSceneMusicTests
{
    private const string CatalogAssetPath = "Assets/Project/Data/Audio/ProjectAudioCatalog.asset";

    [Test]
    public void GetSceneMusicClip_MapsKnownScenesToCatalogEntries()
    {
        ProjectAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectAudioCatalog>(CatalogAssetPath);
        Assert.That(catalog, Is.Not.Null, "ProjectAudioCatalog.asset debe existir para validar el routing de musica.");

        GameObject audioManagerObject = new GameObject("AudioManagerTest");
        AudioManager audioManager = audioManagerObject.AddComponent<AudioManager>();

        try
        {
            SetPrivateField(audioManager, "catalog", catalog);

            Assert.That(audioManager.GetSceneMusicClip("MainMenu"), Is.SameAs(catalog.Music.mainMenu));
            Assert.That(audioManager.GetSceneMusicClip("GamePlay"), Is.SameAs(catalog.Music.gameplay));
            Assert.That(audioManager.GetSceneMusicClip("EndMenu"), Is.SameAs(catalog.Music.endMenu));
            Assert.That(audioManager.GetSceneMusicClip("UnknownScene"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(audioManagerObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"No se encontro el campo privado '{fieldName}'.");
        field.SetValue(target, value);
    }
}
