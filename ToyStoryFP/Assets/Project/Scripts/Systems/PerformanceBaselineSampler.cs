using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Muestreador simple de rendimiento para obtener baseline rapido por escena.
/// Recomendado: colocarlo temporalmente en cada escena durante la fase de auditoria.
/// </summary>
[DisallowMultipleComponent]
public class PerformanceBaselineSampler : MonoBehaviour
{
    [Header("Muestreo")]
    [SerializeField] private bool iniciarAutomaticamente = true;
    [SerializeField] private float duracionMuestreo = 15f;
    [SerializeField] private bool destruirAlCompletar = true;

    private bool muestreoActivo;
    private float tiempoAcumulado;
    private int framesContados;
    private float peorFrameMs;
    private int gc0Inicial;
    private int gc1Inicial;
    private int gc2Inicial;

    // Arranca la configuracion inicial del componente.
    private void Start()
    {
        if (iniciarAutomaticamente)
        {
            IniciarMuestreo();
        }
    }

    // Actualiza la logica en cada frame.
    private void Update()
    {
        if (!muestreoActivo)
        {
            return;
        }

        float delta = Time.unscaledDeltaTime;
        tiempoAcumulado += delta;
        framesContados++;

        float frameMs = delta * 1000f;
        if (frameMs > peorFrameMs)
        {
            peorFrameMs = frameMs;
        }

        if (tiempoAcumulado >= Mathf.Max(1f, duracionMuestreo))
        {
            FinalizarMuestreo();
        }
    }

    [ContextMenu("Iniciar Muestreo")]
    // Gestiona iniciar muestreo.
    public void IniciarMuestreo()
    {
        muestreoActivo = true;
        tiempoAcumulado = 0f;
        framesContados = 0;
        peorFrameMs = 0f;
        gc0Inicial = System.GC.CollectionCount(0);
        gc1Inicial = System.GC.CollectionCount(1);
        gc2Inicial = System.GC.CollectionCount(2);
    }

    [ContextMenu("Finalizar Muestreo")]
    // Gestiona finalizar muestreo.
    public void FinalizarMuestreo()
    {
        if (!muestreoActivo)
        {
            return;
        }

        muestreoActivo = false;

        float promedioFps = tiempoAcumulado > 0.0001f ? framesContados / tiempoAcumulado : 0f;
        float fpsMinAproximado = peorFrameMs > 0.01f ? 1000f / peorFrameMs : 0f;
        int gc0 = System.GC.CollectionCount(0) - gc0Inicial;
        int gc1 = System.GC.CollectionCount(1) - gc1Inicial;
        int gc2 = System.GC.CollectionCount(2) - gc2Inicial;

        string escena = SceneManager.GetActiveScene().name;
        string reporte =
            $"Escena={escena}, Duracion={tiempoAcumulado:F2}s, FPSPromedio={promedioFps:F1}, FPSMinAprox={fpsMinAproximado:F1}, " +
            $"PeorFrameMs={peorFrameMs:F2}, GC0={gc0}, GC1={gc1}, GC2={gc2}";

        GameDebug.Info("BaselineRendimiento", reporte, this);

        if (destruirAlCompletar)
        {
            Destroy(this);
        }
    }
}
