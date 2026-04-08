#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea y valida la estructura base de carpetas del proyecto.
/// Se ejecuta desde menu para que la migracion sea controlada.
/// </summary>
public static class ProjectStructureBootstrapper
{
    private static readonly string[] CarpetasObjetivo =
    {
        "Assets/Project",
        "Assets/Project/Scripts",
        "Assets/Project/Scripts/Core",
        "Assets/Project/Scripts/UI",
        "Assets/Project/Scripts/Player",
        "Assets/Project/Scripts/Enemy",
        "Assets/Project/Scripts/Gameplay",
        "Assets/Project/Scripts/Systems",
        "Assets/Project/Scripts/Editor",
        "Assets/Project/Scripts/Tests",
        "Assets/Project/Scenes",
        "Assets/Project/Prefabs",
        "Assets/Project/Art",
        "Assets/Project/Art/Materials",
        "Assets/Project/Art/Fonts",
        "Assets/Project/Art/Sprites",
        "Assets/Project/Art/Models",
        "Assets/Project/Audio",
        "Assets/Project/Audio/Music",
        "Assets/Project/Audio/SFX",
        "Assets/Project/Audio/Sounds",
        "Assets/Project/Settings",
        "Assets/Project/Settings/RenderPipeline",
        "Assets/ThirdParty",
        "Assets/ThirdParty/PackAssets"
    };

    [MenuItem("Tools/ToyStory/Proyecto/Estructura/Crear O Validar Estructura Base")]
    // Gestiona crear o validar estructura.
    public static void CrearOValidarEstructura()
    {
        int creadas = 0;

        for (int i = 0; i < CarpetasObjetivo.Length; i++)
        {
            string ruta = CarpetasObjetivo[i];

            if (AssetDatabase.IsValidFolder(ruta))
            {
                continue;
            }

            CrearRutaRecursiva(ruta);
            creadas++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (creadas == 0)
        {
            GameDebug.Info("EstructuraProyecto", "La estructura base ya estaba correcta.");
            return;
        }

        GameDebug.Info(
            "EstructuraProyecto",
            $"Se crearon {creadas} carpetas nuevas. Revisar migracion de assets legacy en el editor.");
    }

    // Gestiona crear ruta recursiva.
    private static void CrearRutaRecursiva(string ruta)
    {
        string[] partes = ruta.Split('/');
        string actual = partes[0];

        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = $"{actual}/{partes[i]}";

            if (!AssetDatabase.IsValidFolder(siguiente))
            {
                AssetDatabase.CreateFolder(actual, partes[i]);
            }

            actual = siguiente;
        }
    }
}
#endif
