using System.Diagnostics;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

/// <summary>
/// Envoltura centralizada para logs del juego.
/// Estandariza formato en espanol y evita repetir cadenas en cada sistema.
/// </summary>
public static class GameDebug
{
    private const string SistemaPorDefecto = "General";

    /// <summary>
    /// Permite activar/desactivar logs informativos sin perder warnings/errores.
    /// </summary>
    public static bool MostrarLogsInfo { get; set; } = true;

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Info(string sistema, string mensaje, Object contexto = null)
    {
        if (!MostrarLogsInfo)
        {
            return;
        }

        string textoFinal = FormatearMensaje(sistema, mensaje);

        if (contexto != null)
        {
            UnityDebug.Log(textoFinal, contexto);
            return;
        }

        UnityDebug.Log(textoFinal);
    }

    public static void Advertencia(string sistema, string mensaje, Object contexto = null)
    {
        string textoFinal = FormatearMensaje(sistema, mensaje);

        if (contexto != null)
        {
            UnityDebug.LogWarning(textoFinal, contexto);
            return;
        }

        UnityDebug.LogWarning(textoFinal);
    }

    public static void Error(string sistema, string mensaje, Object contexto = null)
    {
        string textoFinal = FormatearMensaje(sistema, mensaje);

        if (contexto != null)
        {
            UnityDebug.LogError(textoFinal, contexto);
            return;
        }

        UnityDebug.LogError(textoFinal);
    }

    private static string FormatearMensaje(string sistema, string mensaje)
    {
        string sistemaSeguro = string.IsNullOrWhiteSpace(sistema) ? SistemaPorDefecto : sistema.Trim();
        string mensajeSeguro = string.IsNullOrWhiteSpace(mensaje) ? "(sin mensaje)" : mensaje.Trim();
        return $"[{sistemaSeguro}] {mensajeSeguro}";
    }
}
