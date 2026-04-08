using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndMenuUI : MonoBehaviour
{
    [Header("Paneles (no elementos sueltos)")]
    public GameObject gameOverPanel;   
    public GameObject creditsPanel;    
    public GameObject buttonsPanel;    
    
    [Header("Configuración")]
    public float creditsDisplayTime = 4f;
    public float fadeDuration = 0.5f;
    
    void Start()
    {
        StartCoroutine(EndMenuSequence());
    }
    
    IEnumerator EndMenuSequence()
    {
        
        gameOverPanel.SetActive(true);
        creditsPanel.SetActive(false);
        buttonsPanel.SetActive(false);
        
        yield return new WaitForSeconds(2f);
        
        
        yield return StartCoroutine(FadeOut(gameOverPanel));
        gameOverPanel.SetActive(false);
        
      
        creditsPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(creditsPanel));
        
        yield return new WaitForSeconds(creditsDisplayTime);
        
        
        yield return StartCoroutine(FadeOut(creditsPanel));
        creditsPanel.SetActive(false);
        
        
        buttonsPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(buttonsPanel));
    }
    
    IEnumerator FadeOut(GameObject obj)
    {
        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null) group = obj.AddComponent<CanvasGroup>();
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = 0f;
    }
    
    IEnumerator FadeIn(GameObject obj)
    {
        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null) group = obj.AddComponent<CanvasGroup>();
        
        group.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = elapsed / fadeDuration;
            yield return null;
        }
        group.alpha = 1f;
    }
}