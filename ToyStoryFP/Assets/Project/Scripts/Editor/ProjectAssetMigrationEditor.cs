#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;

/// <summary>
/// Migrador de carpetas legacy a estructura moderna usando APIs de Unity.
/// Importante: al usar AssetDatabase.MoveAsset se conservan GUID y referencias.
/// </summary>
public static class ProjectAssetMigrationEditor
{
    private readonly struct MigrationRule
    {
        public MigrationRule(string sourcePath, string targetPath)
        {
            SourcePath = sourcePath;
            TargetPath = targetPath;
        }

        public string SourcePath { get; }
        public string TargetPath { get; }
    }

    private static readonly MigrationRule[] Rules =
    {
        new MigrationRule("Assets/___Scripts", "Assets/Project/Scripts"),
        new MigrationRule("Assets/___Scenes", "Assets/Project/Scenes"),
        new MigrationRule("Assets/__Prefabs", "Assets/Project/Prefabs"),
        new MigrationRule("Assets/\u00F1_PackAssets", "Assets/ThirdParty/PackAssets"),
        new MigrationRule("Assets/_Materials", "Assets/Project/Art/Materials"),
        new MigrationRule("Assets/____Recursos/Audio", "Assets/Project/Audio"),
        new MigrationRule("Assets/____Recursos/Fonts", "Assets/Project/Art/Fonts"),
        new MigrationRule("Assets/____Recursos/Sprites", "Assets/Project/Art/Sprites"),
        new MigrationRule("Assets/____Recursos/FBX", "Assets/Project/Art/Models"),
        new MigrationRule("Assets/\u00F1_Settings", "Assets/Project/Settings/RenderPipeline")
    };

    private static readonly string[] LegacyRootFolders =
    {
        "Assets/___Scripts",
        "Assets/___Scenes",
        "Assets/__Prefabs",
        "Assets/\u00F1_PackAssets",
        "Assets/_Materials",
        "Assets/____Recursos",
        "Assets/\u00F1_Settings"
    };

    [MenuItem("Tools/ToyStory/Proyecto/Migracion/Previsualizar Migracion Legacy -> Project")]
    // Gestiona vista previa migracion.
    public static void PreviewMigration()
    {
        string report = GenerateMigrationReport(previewOnly: true);
        GameDebug.Info("MigracionProyecto", report);
    }

    [MenuItem("Tools/ToyStory/Proyecto/Migracion/Ejecutar Migracion Legacy -> Project")]
    // Gestiona execute migracion.
    public static void ExecuteMigration()
    {
        string migrationReport = GenerateMigrationReport(previewOnly: false);
        string cleanupReport = CleanupLegacyFolders();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameDebug.Info("MigracionProyecto", $"{migrationReport}\n\n{cleanupReport}");
    }

    [MenuItem("Tools/ToyStory/Proyecto/Migracion/Limpiar Carpetas Legacy")]
    // Gestiona execute legacy limpieza only.
    public static void ExecuteLegacyCleanupOnly()
    {
        string report = CleanupLegacyFolders();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameDebug.Info("MigracionProyecto", report);
    }

    // Gestiona generate migracion reporte.
    public static string GenerateMigrationReport(bool previewOnly)
    {
        List<string> lines = new List<string>();
        int moved = 0;
        int warnings = 0;
        string mode = previewOnly ? "PREVIEW" : "APLICADO";
        lines.Add($"Modo: {mode}");

        for (int i = 0; i < Rules.Length; i++)
        {
            MigrationRule rule = Rules[i];

            if (!AssetDatabase.IsValidFolder(rule.SourcePath))
            {
                lines.Add($"- Omitido (no existe): {rule.SourcePath}");
                continue;
            }

            EnsureFolderExists(rule.TargetPath);
            string[] childGuids = AssetDatabase.FindAssets(string.Empty, new[] { rule.SourcePath });

            for (int j = 0; j < childGuids.Length; j++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(childGuids[j]);

                if (string.IsNullOrWhiteSpace(assetPath) || assetPath == rule.SourcePath || assetPath.EndsWith(".meta"))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                string relative = assetPath.Substring(rule.SourcePath.Length).TrimStart('/');
                string target = $"{rule.TargetPath}/{relative}";
                string targetFolder = Path.GetDirectoryName(target)?.Replace("\\", "/");

                if (!string.IsNullOrWhiteSpace(targetFolder))
                {
                    EnsureFolderExists(targetFolder);
                }

                if (previewOnly)
                {
                    lines.Add($"- MOVER: {assetPath} -> {target}");
                    moved++;
                    continue;
                }

                string error = AssetDatabase.MoveAsset(assetPath, target);

                if (string.IsNullOrWhiteSpace(error))
                {
                    lines.Add($"- OK: {assetPath} -> {target}");
                    moved++;
                }
                else
                {
                    warnings++;
                    lines.Add($"- WARN: {assetPath} -> {target} | {error}");
                }
            }
        }

        lines.Add($"Total items: {moved}, warnings: {warnings}");
        return string.Join("\n", lines);
    }

    // Gestiona limpieza legacy carpetas.
    public static string CleanupLegacyFolders()
    {
        List<string> lines = new List<string>();
        int deleted = 0;
        int skipped = 0;
        int warnings = 0;

        lines.Add("=== Limpieza Legacy ===");

        for (int i = 0; i < LegacyRootFolders.Length; i++)
        {
            string folderPath = LegacyRootFolders[i];

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                lines.Add($"- Omitido (no existe): {folderPath}");
                continue;
            }

            if (HasNonMetaFiles(folderPath))
            {
                lines.Add($"- Conservado (tiene archivos reales): {folderPath}");
                skipped++;
                continue;
            }

            bool deleteOk = AssetDatabase.DeleteAsset(folderPath);

            if (deleteOk)
            {
                lines.Add($"- Eliminado: {folderPath}");
                deleted++;
            }
            else
            {
                lines.Add($"- WARN: No se pudo eliminar {folderPath}");
                warnings++;
            }
        }

        lines.Add($"Legacy eliminadas: {deleted}, conservadas: {skipped}, warnings: {warnings}");
        return string.Join("\n", lines);
    }

    // Asegura carpeta exists.
    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    // Comprueba si non meta archivos.
    private static bool HasNonMetaFiles(string assetFolderPath)
    {
        if (string.IsNullOrWhiteSpace(assetFolderPath))
        {
            return false;
        }

        string absolutePath = Path.GetFullPath(assetFolderPath);

        if (!Directory.Exists(absolutePath))
        {
            return false;
        }

        string[] files = Directory.GetFiles(absolutePath, "*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];

            if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
#endif
