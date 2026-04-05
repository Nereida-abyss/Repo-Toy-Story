using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UIFadeUtility
{
    public sealed class FadeTarget
    {
        public Canvas Canvas { get; }
        public CanvasGroup Group { get; }
        public bool CreatedCanvasGroup { get; }
        public float OriginalAlpha { get; }
        public bool OriginalInteractable { get; }
        public bool OriginalBlocksRaycasts { get; }
        public bool OriginalIgnoreParentGroups { get; }

        public FadeTarget(Canvas canvas, CanvasGroup group, bool createdCanvasGroup)
        {
            Canvas = canvas;
            Group = group;
            CreatedCanvasGroup = createdCanvasGroup;
            OriginalAlpha = group.alpha;
            OriginalInteractable = group.interactable;
            OriginalBlocksRaycasts = group.blocksRaycasts;
            OriginalIgnoreParentGroups = group.ignoreParentGroups;
        }

        public void PrepareForFade()
        {
            if (Group == null)
            {
                return;
            }

            Group.interactable = false;
            Group.blocksRaycasts = false;
        }

        public void SetAlpha(float alpha)
        {
            if (Group == null)
            {
                return;
            }

            Group.alpha = alpha;
        }

        public void Restore()
        {
            if (Group == null)
            {
                return;
            }

            Group.alpha = OriginalAlpha;
            Group.interactable = OriginalInteractable;
            Group.blocksRaycasts = OriginalBlocksRaycasts;
            Group.ignoreParentGroups = OriginalIgnoreParentGroups;
        }

        public void Cleanup()
        {
            if (CreatedCanvasGroup && Group != null)
            {
                Object.Destroy(Group);
            }
        }
    }

    public static List<FadeTarget> ResolveActiveCanvasTargets(Scene scene)
    {
        List<FadeTarget> targets = new List<FadeTarget>();
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];

            if (canvas == null || !canvas.isActiveAndEnabled)
            {
                continue;
            }

            if (canvas.gameObject.scene != scene)
            {
                continue;
            }

            if (canvas.rootCanvas != canvas)
            {
                continue;
            }

            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            bool createdCanvasGroup = false;

            if (group == null)
            {
                group = canvas.gameObject.AddComponent<CanvasGroup>();
                createdCanvasGroup = true;
            }

            FadeTarget target = new FadeTarget(canvas, group, createdCanvasGroup);
            target.PrepareForFade();
            targets.Add(target);
        }

        return targets;
    }
}
