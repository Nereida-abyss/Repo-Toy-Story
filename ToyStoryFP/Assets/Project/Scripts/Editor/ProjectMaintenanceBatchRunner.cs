#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runner para ejecutar la segunda pasada en modo batch:
/// preview -> migracion -> organizacion -> validacion.
/// </summary>
public static class ProjectMaintenanceBatchRunner
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Project/Scenes/MainMenu.unity",
        "Assets/Project/Scenes/GamePlay.unity",
        "Assets/Project/Scenes/EndMenu.unity"
    };

    private static readonly string[] PrefabPaths =
    {
        "Assets/Project/Prefabs/Panel/PanelSetting.prefab",
        "Assets/Project/Prefabs/Player/PlayerController.prefab"
    };

    [MenuItem("Tools/ToyStory/Proyecto/Mantenimiento/Staged Safe/1) Preview")]
    public static void RunStagedSafePreview()
    {
        ProjectStructureBootstrapper.CrearOValidarEstructura();

        StringBuilder report = new StringBuilder();
        report.AppendLine("=== Fase A: Preview ===");
        report.AppendLine(ProjectAssetMigrationEditor.GenerateMigrationReport(previewOnly: true));
        report.AppendLine();
        report.AppendLine("=== Validacion de Jerarquia (sin cambios) ===");

        foreach (string scenePath in ScenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !System.IO.File.Exists(scenePath))
            {
                report.AppendLine($"- Escena no encontrada (omitida): {scenePath}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string sceneReport = SceneHierarchyOrganizer.AnalyzeScene(scene, aplicarCambios: false, aplicarRenombresObjetivo: false);
            report.AppendLine($"- {scene.name}: {sceneReport}");
        }

        GameDebug.Info("MantenimientoProyecto", report.ToString());
    }

    [MenuItem("Tools/ToyStory/Proyecto/Mantenimiento/Staged Safe/2) Apply Migration + Hierarchy")]
    public static void RunStagedSafeApply()
    {
        ProjectStructureBootstrapper.CrearOValidarEstructura();

        StringBuilder report = new StringBuilder();
        report.AppendLine("=== Fase B + C: Aplicacion ===");
        report.AppendLine(ProjectAssetMigrationEditor.GenerateMigrationReport(previewOnly: false));
        report.AppendLine();
        report.AppendLine(ProjectAssetMigrationEditor.CleanupLegacyFolders());

        SceneHierarchyOrganizer.OrganizarEscenasPrincipales();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.AppendLine("Organizacion de jerarquia aplicada en escenas principales.");
        GameDebug.Info("MantenimientoProyecto", report.ToString());
    }

    [MenuItem("Tools/ToyStory/Proyecto/Mantenimiento/Staged Safe/3) Validation")]
    public static void RunStagedSafeValidation()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== Fase D: Validacion ===");

        int missingInScenes = 0;
        int missingInPrefabs = 0;
        int namingIssues = 0;
        int legacyFolderIssues = CountLegacyFoldersStillPresent();

        foreach (string scenePath in ScenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !System.IO.File.Exists(scenePath))
            {
                report.AppendLine($"- Escena no encontrada (omitida): {scenePath}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int missingCount = CountMissingScriptsInScene(scene);
            missingInScenes += missingCount;
            report.AppendLine($"- Escena {scene.name}: MissingScripts={missingCount}");
            namingIssues += ValidateNamingExpectations(scene, report);
        }

        foreach (string prefabPath in PrefabPaths)
        {
            if (string.IsNullOrWhiteSpace(prefabPath) || !System.IO.File.Exists(prefabPath))
            {
                report.AppendLine($"- Prefab no encontrado (omitido): {prefabPath}");
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            int missingCount = CountMissingScriptsInHierarchy(root != null ? root.transform : null);
            missingInPrefabs += missingCount;
            report.AppendLine($"- Prefab {prefabPath}: MissingScripts={missingCount}");

            if (root != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        int transformFindUsages = CountTransformFindUsages();
        report.AppendLine($"- Uso de transform.Find(...) en scripts de juego: {transformFindUsages}");
        report.AppendLine($"- Incidencias de naming/hierarchy: {namingIssues}");
        report.AppendLine($"- Carpetas legacy residuales: {legacyFolderIssues}");
        report.AppendLine($"- Total MissingScripts (Escenas): {missingInScenes}");
        report.AppendLine($"- Total MissingScripts (Prefabs): {missingInPrefabs}");

        if (missingInScenes == 0 &&
            missingInPrefabs == 0 &&
            transformFindUsages == 0 &&
            namingIssues == 0 &&
            legacyFolderIssues == 0)
        {
            report.AppendLine("VALIDACION OK");
        }
        else
        {
            report.AppendLine("VALIDACION CON HALLAZGOS");
        }

        GameDebug.Info("MantenimientoProyecto", report.ToString());
    }

    [MenuItem("Tools/ToyStory/Proyecto/Mantenimiento/Staged Safe/Run Full")]
    public static void RunStagedSafeFull()
    {
        RunStagedSafePreview();
        RunStagedSafeApply();
        RunStagedSafeValidation();
    }

    private static int CountMissingScriptsInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return 0;
        }

        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            count += CountMissingScriptsInHierarchy(roots[i].transform);
        }

        return count;
    }

    private static int CountMissingScriptsInHierarchy(Transform root)
    {
        if (root == null)
        {
            return 0;
        }

        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root.gameObject);

        for (int i = 0; i < root.childCount; i++)
        {
            count += CountMissingScriptsInHierarchy(root.GetChild(i));
        }

        return count;
    }

    private static int CountTransformFindUsages()
    {
        string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Project/Scripts" });
        int count = 0;
        Regex pattern = new Regex(@"\btransform\.Find\s*\(", RegexOptions.Compiled);

        for (int i = 0; i < scriptGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);

            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (path.Contains("/Editor/") || path.EndsWith("ProjectMaintenanceBatchRunner.cs"))
            {
                continue;
            }

            string code = System.IO.File.ReadAllText(path);
            count += pattern.Matches(code).Count;
        }

        return count;
    }

    private static int ValidateNamingExpectations(Scene scene, StringBuilder report)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return 0;
        }

        int issues = 0;
        Dictionary<string, Transform> rootsByName = BuildRootLookup(scene);

        string[] requiredBaseRoots =
        {
            "__Managers",
            "__Systems",
            "__World",
            "__UI",
            "__Cameras",
            "__Debug"
        };

        for (int i = 0; i < requiredBaseRoots.Length; i++)
        {
            string rootName = requiredBaseRoots[i];
            if (!rootsByName.ContainsKey(rootName))
            {
                report.AppendLine($"  * Falta raiz requerida: {rootName}");
                issues++;
            }
        }

        if (string.Equals(scene.name, "GamePlay", System.StringComparison.OrdinalIgnoreCase) &&
            !rootsByName.ContainsKey("__Gameplay"))
        {
            report.AppendLine("  * Falta raiz requerida: __Gameplay");
            issues++;
        }

        issues += ValidateExpectedChildren(rootsByName, "__Managers", new[] { "GameManager", "AudioManager" }, report);
        issues += ValidateExpectedChildren(rootsByName, "__Systems", new[] { "EventSystem" }, report);
        issues += ValidateExpectedChildren(rootsByName, "__World", new[] { "Room", "DirectionalLight", "LightBlocksObjects" }, report);

        if (string.Equals(scene.name, "MainMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            issues += ValidateExpectedChildren(rootsByName, "__UI", new[] { "MainMenuCanvas" }, report);
            issues += ValidateExpectedChildren(rootsByName, "__Cameras", new[] { "MainCamera" }, report);
        }
        else if (string.Equals(scene.name, "EndMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            issues += ValidateExpectedChildren(rootsByName, "__UI", new[] { "EndMenuCanvas" }, report);
            issues += ValidateExpectedChildren(rootsByName, "__Cameras", new[] { "MainCamera" }, report);
        }
        else if (string.Equals(scene.name, "GamePlay", System.StringComparison.OrdinalIgnoreCase))
        {
            issues += ValidateExpectedChildren(rootsByName, "__World", new[] { "GlobalVolume" }, report);
            issues += ValidateExpectedChildren(rootsByName, "__Gameplay", new[] { "WaveSystem" }, report);
        }

        return issues;
    }

    private static int ValidateExpectedChildren(
        Dictionary<string, Transform> rootsByName,
        string rootName,
        string[] expectedChildren,
        StringBuilder report)
    {
        if (!rootsByName.TryGetValue(rootName, out Transform root) || root == null)
        {
            return 0;
        }

        HashSet<string> childNames = new HashSet<string>(System.StringComparer.Ordinal);

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null)
            {
                childNames.Add(child.name);
            }
        }

        int issues = 0;

        for (int i = 0; i < expectedChildren.Length; i++)
        {
            string expected = expectedChildren[i];
            if (!childNames.Contains(expected))
            {
                report.AppendLine($"  * Falta '{expected}' dentro de {rootName}");
                issues++;
            }
        }

        return issues;
    }

    private static Dictionary<string, Transform> BuildRootLookup(Scene scene)
    {
        Dictionary<string, Transform> result = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root != null && !result.ContainsKey(root.name))
            {
                result[root.name] = root.transform;
            }
        }

        return result;
    }

    private static int CountLegacyFoldersStillPresent()
    {
        string[] legacyFolders =
        {
            "Assets/___Scripts",
            "Assets/___Scenes",
            "Assets/__Prefabs",
            "Assets/\u00F1_PackAssets",
            "Assets/_Materials",
            "Assets/____Recursos",
            "Assets/\u00F1_Settings"
        };

        int count = 0;

        for (int i = 0; i < legacyFolders.Length; i++)
        {
            if (AssetDatabase.IsValidFolder(legacyFolders[i]))
            {
                count++;
            }
        }

        return count;
    }
}
#endif
