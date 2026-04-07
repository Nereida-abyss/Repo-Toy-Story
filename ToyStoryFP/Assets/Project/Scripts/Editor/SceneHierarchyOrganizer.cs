#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta de editor para ordenar la jerarquia de escenas siguiendo una convencion unica.
/// No toca gameplay; solo organiza raices para mejorar mantenimiento.
/// </summary>
public static class SceneHierarchyOrganizer
{
    private static readonly string[] EscenasPrincipales =
    {
        "Assets/Project/Scenes/MainMenu.unity",
        "Assets/Project/Scenes/GamePlay.unity",
        "Assets/Project/Scenes/EndMenu.unity"
    };

    private static readonly Dictionary<string, string> RootRenameMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "MainMenu_Canvas", "MainMenuCanvas" },
        { "EndMenu_Canvas", "EndMenuCanvas" },
        { "Main Camera", "MainCamera" },
        { "Directional Light", "DirectionalLight" },
        { "Global Volume", "GlobalVolume" },
        { "ROOM", "Room" }
    };

    private const string RootManagers = "__Managers";
    private const string RootSystems = "__Systems";
    private const string RootWorld = "__World";
    private const string RootGameplay = "__Gameplay";
    private const string RootUi = "__UI";
    private const string RootCameras = "__Cameras";
    private const string RootDebug = "__Debug";

    private static readonly string[] RaicesBase =
    {
        RootManagers,
        RootSystems,
        RootWorld,
        RootUi,
        RootCameras,
        RootDebug
    };

    [MenuItem("Tools/ToyStory/Proyecto/Jerarquia/Validar Escena Activa")]
    public static void ValidarEscenaActiva()
    {
        Scene escena = SceneManager.GetActiveScene();
        string reporte = AnalizarEscena(escena, aplicarCambios: false, aplicarRenombresObjetivo: false);
        GameDebug.Info("Jerarquia", reporte);
    }

    [MenuItem("Tools/ToyStory/Proyecto/Jerarquia/Organizar Escena Activa")]
    public static void OrganizarEscenaActiva()
    {
        Scene escena = SceneManager.GetActiveScene();
        string reporte = AnalizarEscena(escena, aplicarCambios: true, aplicarRenombresObjetivo: true);
        GameDebug.Info("Jerarquia", reporte);
    }

    [MenuItem("Tools/ToyStory/Proyecto/Jerarquia/Organizar MainMenu + GamePlay + EndMenu")]
    public static void OrganizarEscenasPrincipales()
    {
        string escenaOriginal = SceneManager.GetActiveScene().path;
        StringBuilder resumen = new StringBuilder();
        resumen.AppendLine("Organizacion de jerarquia completada:");

        for (int i = 0; i < EscenasPrincipales.Length; i++)
        {
            string escenaPath = EscenasPrincipales[i];

            if (string.IsNullOrWhiteSpace(escenaPath) || !System.IO.File.Exists(escenaPath))
            {
                continue;
            }

            Scene escena = EditorSceneManager.OpenScene(escenaPath, OpenSceneMode.Single);
            string reporte = AnalizarEscena(escena, aplicarCambios: true, aplicarRenombresObjetivo: true);
            resumen.AppendLine($"- {escena.name}: {reporte}");
        }

        if (!string.IsNullOrWhiteSpace(escenaOriginal))
        {
            EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
        }

        GameDebug.Info("Jerarquia", resumen.ToString());
    }

    public static string AnalyzeScene(Scene escena, bool aplicarCambios, bool aplicarRenombresObjetivo)
    {
        return AnalizarEscena(escena, aplicarCambios, aplicarRenombresObjetivo);
    }

    private static string AnalizarEscena(Scene escena, bool aplicarCambios, bool aplicarRenombresObjetivo)
    {
        if (!escena.IsValid() || !escena.isLoaded)
        {
            return "No se pudo analizar la escena porque no esta cargada.";
        }

        Dictionary<string, Transform> raices = CrearRaicesConvencion(escena, aplicarCambios);
        GameObject[] rootObjects = escena.GetRootGameObjects();

        int movimientos = 0;
        int renombres = 0;
        int legacyDetectados = 0;

        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject root = rootObjects[i];

            if (root == null || raices.ContainsKey(root.name))
            {
                continue;
            }

            string nombreOriginal = root.name;
            bool esLegacy = TieneNombreLegacy(nombreOriginal);

            if (esLegacy)
            {
                legacyDetectados++;
            }

            string categoria = ResolverCategoriaRaiz(root, escena.name);

            if (aplicarCambios)
            {
                Transform destino = raices[categoria];

                if (root.transform.parent != destino)
                {
                    Undo.SetTransformParent(root.transform, destino, "Organizar jerarquia de escena");
                    movimientos++;
                }

                if (esLegacy)
                {
                    string nombreLimpio = NormalizarNombre(nombreOriginal);

                    if (!string.Equals(nombreOriginal, nombreLimpio, StringComparison.Ordinal))
                    {
                        Undo.RecordObject(root, "Renombrar objeto legacy");
                        root.name = nombreLimpio;
                        renombres++;
                    }
                }
            }
        }

        if (aplicarCambios)
        {
            if (aplicarRenombresObjetivo)
            {
                renombres += AplicarRenombresObjetivo(escena, raices);
            }

            OrdenarRaices(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        return $"movimientos={movimientos}, renombres={renombres}, legacyDetectados={legacyDetectados}";
    }

    private static int AplicarRenombresObjetivo(Scene escena, Dictionary<string, Transform> raices)
    {
        int renombrados = 0;

        foreach (KeyValuePair<string, Transform> entry in raices)
        {
            Transform categoriaRoot = entry.Value;

            if (categoriaRoot == null)
            {
                continue;
            }

            for (int i = 0; i < categoriaRoot.childCount; i++)
            {
                Transform child = categoriaRoot.GetChild(i);

                if (child == null || string.IsNullOrWhiteSpace(child.name))
                {
                    continue;
                }

                if (!RootRenameMap.TryGetValue(child.name, out string nombreObjetivo))
                {
                    continue;
                }

                if (string.Equals(child.name, nombreObjetivo, StringComparison.Ordinal))
                {
                    continue;
                }

                Undo.RecordObject(child.gameObject, "Renombrar raiz objetivo");
                child.name = nombreObjetivo;
                renombrados++;
            }
        }

        return renombrados;
    }

    private static Dictionary<string, Transform> CrearRaicesConvencion(Scene escena, bool aplicarCambios)
    {
        Dictionary<string, Transform> resultado = new Dictionary<string, Transform>(StringComparer.Ordinal);
        List<string> nombresRaices = new List<string>(RaicesBase);

        if (string.Equals(escena.name, "GamePlay", StringComparison.OrdinalIgnoreCase))
        {
            nombresRaices.Insert(3, RootGameplay);
        }

        GameObject[] roots = escena.GetRootGameObjects();

        for (int i = 0; i < nombresRaices.Count; i++)
        {
            string nombreRaiz = nombresRaices[i];
            Transform existente = roots.FirstOrDefault(r => r.name == nombreRaiz)?.transform;

            if (existente != null)
            {
                resultado[nombreRaiz] = existente;
                continue;
            }

            if (!aplicarCambios)
            {
                continue;
            }

            GameObject nuevoRoot = new GameObject(nombreRaiz);
            SceneManager.MoveGameObjectToScene(nuevoRoot, escena);
            Undo.RegisterCreatedObjectUndo(nuevoRoot, "Crear raiz de jerarquia");
            resultado[nombreRaiz] = nuevoRoot.transform;
        }

        return resultado;
    }

    private static void OrdenarRaices(Scene escena)
    {
        string[] ordenRoots =
        {
            RootManagers,
            RootSystems,
            RootWorld,
            RootGameplay,
            RootUi,
            RootCameras,
            RootDebug
        };

        Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < ordenRoots.Length; i++)
        {
            indices[ordenRoots[i]] = i;
        }

        GameObject[] roots = escena.GetRootGameObjects();
        Array.Sort(
            roots,
            (a, b) =>
            {
                int indexA = indices.TryGetValue(a.name, out int ia) ? ia : int.MaxValue;
                int indexB = indices.TryGetValue(b.name, out int ib) ? ib : int.MaxValue;
                return indexA.CompareTo(indexB);
            });

        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].transform.SetSiblingIndex(i);
        }
    }

    private static string ResolverCategoriaRaiz(GameObject obj, string sceneName)
    {
        string nombre = obj.name.ToLowerInvariant();
        bool esGamePlay = string.Equals(sceneName, "GamePlay", StringComparison.OrdinalIgnoreCase);

        if (esGamePlay &&
            (nombre.Contains("player") ||
             nombre.Contains("enemy") ||
             nombre.Contains("wave") ||
             nombre.Contains("spawn") ||
             nombre.Contains("pickup") ||
             nombre.Contains("weapon")))
        {
            return RootGameplay;
        }

        if (obj.GetComponentInChildren<Canvas>(true) != null ||
            obj.GetComponentInChildren<UnityEngine.UI.Graphic>(true) != null ||
            nombre.Contains("canvas") ||
            nombre.Contains("panel") ||
            nombre.Contains("menu"))
        {
            return RootUi;
        }

        if (obj.GetComponentInChildren<Camera>(true) != null ||
            obj.GetComponentInChildren<AudioListener>(true) != null ||
            nombre.Contains("camera"))
        {
            return RootCameras;
        }

        if (nombre.Contains("manager") || TieneComponenteConSufijo(obj, "Manager"))
        {
            return RootManagers;
        }

        if (nombre.Contains("system") ||
            nombre.Contains("eventsystem") ||
            TieneComponenteConSufijo(obj, "System"))
        {
            return RootSystems;
        }

        if (nombre.Contains("debug") || nombre.Contains("test") || nombre.Contains("gizmo"))
        {
            return RootDebug;
        }

        return RootWorld;
    }

    private static bool TieneComponenteConSufijo(GameObject obj, string sufijo)
    {
        Component[] components = obj.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                continue;
            }

            string nombreTipo = component.GetType().Name;

            if (nombreTipo.EndsWith(sufijo, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TieneNombreLegacy(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return false;
        }

        if (nombre.Contains("___") || nombre.Contains("__") || nombre.Contains("\u00F1"))
        {
            return true;
        }

        for (int i = 0; i < nombre.Length; i++)
        {
            if (nombre[i] > 127)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizarNombre(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
        {
            return "Objeto";
        }

        string sinAcentos = RemoverDiacriticos(original);
        StringBuilder limpio = new StringBuilder(sinAcentos.Length);

        for (int i = 0; i < sinAcentos.Length; i++)
        {
            char c = sinAcentos[i];

            if (char.IsLetterOrDigit(c))
            {
                limpio.Append(c);
                continue;
            }

            limpio.Append(' ');
        }

        string[] partes = limpio
            .ToString()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (partes.Length == 0)
        {
            return "Objeto";
        }

        StringBuilder pascal = new StringBuilder();

        for (int i = 0; i < partes.Length; i++)
        {
            string parte = partes[i];

            if (parte.Length == 0)
            {
                continue;
            }

            pascal.Append(char.ToUpperInvariant(parte[0]));

            if (parte.Length > 1)
            {
                pascal.Append(parte.Substring(1));
            }
        }

        return pascal.ToString();
    }

    private static string RemoverDiacriticos(string texto)
    {
        string normalizado = texto.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalizado.Length);

        for (int i = 0; i < normalizado.Length; i++)
        {
            char c = normalizado[i];
            UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);

            if (categoria != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

}
#endif
