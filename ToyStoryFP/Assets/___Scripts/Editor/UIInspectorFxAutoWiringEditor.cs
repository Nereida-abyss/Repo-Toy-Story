#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIInspectorFxAutoWiringEditor
{
    private const string AutoWireVersionKey = "ToyStory.UIFx.AutoWire.v2";

    private static readonly Color PanelColor = new Color(0.08f, 0.16f, 0.33f, 0.84f);
    private static readonly Color ButtonNormalColor = new Color(1f, 0.95f, 0.78f, 0.96f);
    private static readonly Color ButtonHighlightedColor = new Color(1f, 0.98f, 0.90f, 1f);
    private static readonly Color ButtonPressedColor = new Color(1f, 0.84f, 0.58f, 1f);
    private static readonly Color ButtonDisabledColor = new Color(0.62f, 0.67f, 0.78f, 0.75f);
    private static readonly Color ButtonTextColor = new Color(0.19f, 0.18f, 0.28f, 1f);
    private static readonly Color TitleColor = new Color(1f, 0.97f, 0.79f, 1f);
    private static readonly Color BodyColor = new Color(0.93f, 0.97f, 1f, 1f);
    private static readonly Color SecondaryColor = new Color(0.8f, 0.92f, 1f, 1f);
    private static readonly Color ShadowColor = new Color(0.04f, 0.07f, 0.14f, 0.65f);

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.delayCall += TryAutoWireOnce;
    }

    [MenuItem("Tools/ToyStory/UI/Apply Inspector FX Wiring")]
    public static void ApplyInspectorFxWiring()
    {
        ApplyInspectorFxWiringInternal(verbose: true);
    }

    [MenuItem("Tools/ToyStory/UI/Force Reapply Inspector FX Wiring")]
    public static void ForceReapplyInspectorFxWiring()
    {
        EditorPrefs.DeleteKey(AutoWireVersionKey);
        ApplyInspectorFxWiringInternal(verbose: true);
    }

    private static void TryAutoWireOnce()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            return;
        }

        if (EditorPrefs.GetBool(AutoWireVersionKey, false))
        {
            return;
        }

        ApplyInspectorFxWiringInternal(verbose: false);
    }

    private static void ApplyInspectorFxWiringInternal(bool verbose)
    {
        string originalScenePath = SceneManager.GetActiveScene().path;
        int changes = 0;
        int warnings = 0;

        string[] scenes =
        {
            "Assets/___Scenes/MainMenu.unity",
            "Assets/___Scenes/EndMenu.unity"
        };

        for (int i = 0; i < scenes.Length; i++)
        {
            string scenePath = scenes[i];
            WireScene(scenePath, ref changes, ref warnings);
        }

        string[] prefabs =
        {
            "Assets/__Prefabs/Panel/PanelSetting.prefab",
            "Assets/__Prefabs/Player/PlayerController.prefab"
        };

        for (int i = 0; i < prefabs.Length; i++)
        {
            string prefabPath = prefabs[i];
            WirePrefab(prefabPath, ref changes, ref warnings);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(AutoWireVersionKey, true);

        string resolvedOriginalScenePath = string.IsNullOrWhiteSpace(originalScenePath) ? string.Empty : Path.GetFullPath(originalScenePath);

        if (!string.IsNullOrWhiteSpace(originalScenePath) && File.Exists(resolvedOriginalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        if (verbose)
        {
            Debug.Log($"UI Inspector FX wiring complete. Changes: {changes}, Warnings: {warnings}");
        }
    }

    private static void WireScene(string scenePath, ref int changes, ref int warnings)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            warnings++;
            Debug.LogWarning($"Could not open scene for UI FX wiring: {scenePath}");
            return;
        }

        bool sceneChanged = false;

        if (scenePath.EndsWith("MainMenu.unity"))
        {
            sceneChanged |= WireButtonsInRoot(scene, new[] { "PlayButton", "SettingButton", "ExitButton" }, ref changes, ref warnings);
            sceneChanged |= WirePanelsInRoot(scene, new[] { "MainMenu_Panel", "PanelSetting" }, ref changes, ref warnings);
            sceneChanged |= StyleTextInRoot(scene, new[] { "Title" }, TitleColor, true, ref changes);
        }
        else if (scenePath.EndsWith("EndMenu.unity"))
        {
            sceneChanged |= WireButtonsInRoot(scene, new[] { "RetryButton", "CreditsButton", "ScoreButton", "SettingButton", "ExitButton", "BackButton", "MenuButton" }, ref changes, ref warnings);
            sceneChanged |= WirePanelsInRoot(scene, new[] { "PanelButtons", "PanelCredits", "PanelGameOver", "PanelScore", "PanelSetting" }, ref changes, ref warnings);
            sceneChanged |= StyleTextInRoot(scene, new[] { "TextGameOver", "ScoreTitle", "Thanks", "TextLoser" }, TitleColor, true, ref changes);
            sceneChanged |= StyleTextInRoot(scene, new[] { "BestCoinsText", "BestWaveText", "BestBotsText" }, SecondaryColor, false, ref changes);
        }

        if (sceneChanged)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void WirePrefab(string prefabPath, ref int changes, ref int warnings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        if (root == null)
        {
            warnings++;
            Debug.LogWarning($"Could not open prefab for UI FX wiring: {prefabPath}");
            return;
        }

        bool prefabChanged = false;

        if (prefabPath.EndsWith("PanelSetting.prefab"))
        {
            prefabChanged |= WireButtonsInTransform(root.transform, new[] { "CloseButton", "FullscreenButton", "MuteButton" }, ref changes, ref warnings);
            prefabChanged |= WirePanelsInTransform(root.transform, new[] { "PanelSetting" }, ref changes, ref warnings);
            prefabChanged |= StyleTextInTransform(root.transform, new[] { "FullscreenStateText", "MuteStateText", "LookSensitivityValueText" }, SecondaryColor, false, ref changes);
        }
        else if (prefabPath.EndsWith("PlayerController.prefab"))
        {
            prefabChanged |= WireButtonsInTransform(root.transform, new[] { "ButtonPause", "ButtonResume", "ButtonRetry", "ButtonMainMenu", "ButtonSetting", "ButtonExit" }, ref changes, ref warnings);
            prefabChanged |= WirePanelsInTransform(root.transform, new[] { "PanelPause", "WaveAnnouncementPanel", "NextWavePromptPanel", "WaveTimersPanel" }, ref changes, ref warnings);
            prefabChanged |= StyleTextInTransform(root.transform, new[] { "WaveAnnouncementText" }, TitleColor, true, ref changes);
            prefabChanged |= StyleTextInTransform(root.transform, new[] { "NextWavePromptText", "RoundTimerText", "IntermissionTimerText" }, SecondaryColor, false, ref changes);
            prefabChanged |= StyleTextInTransform(root.transform, new[] { "HealthText", "AmmoText", "ReloadText", "CoinsText" }, BodyColor, false, ref changes);
        }

        if (prefabChanged)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(root);
    }

    private static bool WireButtonsInRoot(Scene scene, string[] buttonNames, ref int changes, ref int warnings)
    {
        bool changed = false;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            changed |= WireButtonsInTransform(roots[i].transform, buttonNames, ref changes, ref warnings);
        }

        return changed;
    }

    private static bool WirePanelsInRoot(Scene scene, string[] panelNames, ref int changes, ref int warnings)
    {
        bool changed = false;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            changed |= WirePanelsInTransform(roots[i].transform, panelNames, ref changes, ref warnings);
        }

        return changed;
    }

    private static bool StyleTextInRoot(Scene scene, string[] textNames, Color color, bool isTitle, ref int changes)
    {
        bool changed = false;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            changed |= StyleTextInTransform(roots[i].transform, textNames, color, isTitle, ref changes);
        }

        return changed;
    }

    private static bool WireButtonsInTransform(Transform root, string[] buttonNames, ref int changes, ref int warnings)
    {
        bool changed = false;

        for (int i = 0; i < buttonNames.Length; i++)
        {
            string targetName = buttonNames[i];
            Transform target = FindByName(root, targetName);

            if (target == null)
            {
                warnings++;
                continue;
            }

            Button button = target.GetComponent<Button>();

            if (button == null)
            {
                warnings++;
                continue;
            }

            UIButtonFx buttonFx = target.GetComponent<UIButtonFx>();

            if (buttonFx == null)
            {
                buttonFx = Undo.AddComponent<UIButtonFx>(target.gameObject);
                changes++;
                changed = true;
            }

            changed |= ApplyButtonStyle(button, ref changes);
            changed |= ApplyButtonFxDefaults(buttonFx, ref changes);
            changed |= ApplyButtonTextStyle(target, ref changes);
        }

        return changed;
    }

    private static bool WirePanelsInTransform(Transform root, string[] panelNames, ref int changes, ref int warnings)
    {
        bool changed = false;

        for (int i = 0; i < panelNames.Length; i++)
        {
            string targetName = panelNames[i];
            Transform target = FindByName(root, targetName);

            if (target == null)
            {
                warnings++;
                continue;
            }

            UIPanelFx panelFx = target.GetComponent<UIPanelFx>();

            if (panelFx == null)
            {
                panelFx = Undo.AddComponent<UIPanelFx>(target.gameObject);
                changes++;
                changed = true;
            }

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = Undo.AddComponent<CanvasGroup>(target.gameObject);
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                changes++;
                changed = true;
            }

            changed |= ApplyPanelFxDefaults(panelFx, canvasGroup, ref changes);

            Image panelImage = target.GetComponent<Image>();

            if (panelImage != null && panelImage.color != PanelColor)
            {
                panelImage.color = PanelColor;
                EditorUtility.SetDirty(panelImage);
                changes++;
                changed = true;
            }
        }

        return changed;
    }

    private static bool ApplyButtonStyle(Button button, ref int changes)
    {
        bool changed = false;
        ColorBlock colors = button.colors;

        if (colors.normalColor != ButtonNormalColor
            || colors.highlightedColor != ButtonHighlightedColor
            || colors.pressedColor != ButtonPressedColor
            || colors.selectedColor != ButtonHighlightedColor
            || colors.disabledColor != ButtonDisabledColor
            || Mathf.Abs(colors.fadeDuration - 0.07f) > 0.0001f
            || Mathf.Abs(colors.colorMultiplier - 1f) > 0.0001f)
        {
            colors.normalColor = ButtonNormalColor;
            colors.highlightedColor = ButtonHighlightedColor;
            colors.pressedColor = ButtonPressedColor;
            colors.selectedColor = ButtonHighlightedColor;
            colors.disabledColor = ButtonDisabledColor;
            colors.fadeDuration = 0.07f;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            EditorUtility.SetDirty(button);
            changes++;
            changed = true;
        }

        return changed;
    }

    private static bool ApplyButtonFxDefaults(UIButtonFx buttonFx, ref int changes)
    {
        SerializedObject serialized = new SerializedObject(buttonFx);
        bool changed = false;

        changed |= SetSerializedColor(serialized, "normalColor", ButtonNormalColor);
        changed |= SetSerializedColor(serialized, "hoverColor", ButtonHighlightedColor);
        changed |= SetSerializedColor(serialized, "pressedColor", ButtonPressedColor);
        changed |= SetSerializedFloat(serialized, "hoverScale", 1.07f);
        changed |= SetSerializedFloat(serialized, "pressedScale", 0.93f);
        changed |= SetSerializedFloat(serialized, "animationSpeed", 17f);
        changed |= SetSerializedBool(serialized, "useAudioManagerFallback", false);
        changed |= SetSerializedBool(serialized, "useSharedAudioSource", true);

        if (changed)
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(buttonFx);
            changes++;
        }

        return changed;
    }

    private static bool ApplyPanelFxDefaults(UIPanelFx panelFx, CanvasGroup canvasGroup, ref int changes)
    {
        SerializedObject serialized = new SerializedObject(panelFx);
        bool changed = false;

        changed |= SetSerializedObject(serialized, "canvasGroup", canvasGroup);
        changed |= SetSerializedBool(serialized, "playOpenOnEnable", true);
        changed |= SetSerializedFloat(serialized, "openDuration", 0.25f);
        changed |= SetSerializedFloat(serialized, "closeDuration", 0.16f);
        changed |= SetSerializedFloat(serialized, "slideOffset", 24f);
        changed |= SetSerializedFloat(serialized, "startScale", 0.94f);
        changed |= SetSerializedFloat(serialized, "closeScale", 0.97f);
        changed |= SetSerializedBool(serialized, "useAudioManagerFallback", false);
        changed |= SetSerializedBool(serialized, "useSharedAudioSource", true);

        if (changed)
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panelFx);
            changes++;
        }

        return changed;
    }

    private static bool ApplyButtonTextStyle(Transform buttonRoot, ref int changes)
    {
        bool changed = false;
        TMP_Text[] texts = buttonRoot.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
            {
                continue;
            }

            bool localChanged = false;

            if (text.color != ButtonTextColor)
            {
                text.color = ButtonTextColor;
                localChanged = true;
            }

            if (text.fontStyle != FontStyles.Bold)
            {
                text.fontStyle = FontStyles.Bold;
                localChanged = true;
            }

            Shadow shadow = text.GetComponent<Shadow>();

            if (shadow != null)
            {
                if (shadow.effectColor != ShadowColor)
                {
                    shadow.effectColor = ShadowColor;
                    localChanged = true;
                }

                if (shadow.effectDistance != new Vector2(2f, -2f))
                {
                    shadow.effectDistance = new Vector2(2f, -2f);
                    localChanged = true;
                }
            }

            if (localChanged)
            {
                EditorUtility.SetDirty(text);
                changes++;
                changed = true;
            }
        }

        return changed;
    }

    private static bool StyleTextInTransform(Transform root, string[] textNames, Color color, bool isTitle, ref int changes)
    {
        bool changed = false;

        for (int i = 0; i < textNames.Length; i++)
        {
            Transform target = FindByName(root, textNames[i]);

            if (target == null)
            {
                continue;
            }

            TMP_Text text = target.GetComponent<TMP_Text>();

            if (text == null)
            {
                continue;
            }

            bool localChanged = false;

            if (text.color != color)
            {
                text.color = color;
                localChanged = true;
            }

            FontStyles targetStyle = isTitle ? FontStyles.Bold : FontStyles.Normal;

            if (text.fontStyle != targetStyle)
            {
                text.fontStyle = targetStyle;
                localChanged = true;
            }

            Shadow shadow = text.GetComponent<Shadow>();

            if (shadow != null)
            {
                Vector2 targetDistance = isTitle ? new Vector2(2.8f, -2.8f) : new Vector2(2f, -2f);

                if (shadow.effectColor != ShadowColor)
                {
                    shadow.effectColor = ShadowColor;
                    localChanged = true;
                }

                if (shadow.effectDistance != targetDistance)
                {
                    shadow.effectDistance = targetDistance;
                    localChanged = true;
                }
            }

            if (localChanged)
            {
                EditorUtility.SetDirty(text);
                changes++;
                changed = true;
            }
        }

        return changed;
    }

    private static Transform FindByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            if (current.name == targetName)
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }

    private static bool SetSerializedBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null || property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static bool SetSerializedFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null || Mathf.Abs(property.floatValue - value) <= 0.0001f)
        {
            return false;
        }

        property.floatValue = value;
        return true;
    }

    private static bool SetSerializedColor(SerializedObject serialized, string propertyName, Color value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null || property.colorValue == value)
        {
            return false;
        }

        property.colorValue = value;
        return true;
    }

    private static bool SetSerializedObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null || property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }
}
#endif
