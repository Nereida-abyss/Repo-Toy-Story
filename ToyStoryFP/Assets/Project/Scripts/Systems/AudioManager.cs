using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clip Arrays")]
    public AudioClip[] musicList;
    public AudioClip[] sfxList;

    [Header("Audio Source References")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")]
    [SerializeField] private bool keepMainMenuMusicInAllScenes = true;
    [SerializeField] private int mainMenuMusicIndex = 0;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.05f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad solo funciona en objetos raiz.
        // Si el manager esta anidado en la jerarquia, lo desacoplamos primero.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        EnsureMainMenuMusic();
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        if (!keepMainMenuMusicInAllScenes)
        {
            return;
        }

        EnsureMainMenuMusic();
    }

    public void PlayMusic(int musicIndex)
    {
        EnsureAudioSources();

        if (musicList == null || musicList.Length == 0)
        {
            GameDebug.Advertencia("Audio", "No hay pistas en musicList para reproducir.", this);
            return;
        }

        if (musicIndex < 0 || musicIndex >= musicList.Length)
        {
            GameDebug.Advertencia("Audio", $"Indice de musica fuera de rango: {musicIndex}", this);
            return;
        }

        if (musicSource == null)
        {
            GameDebug.Advertencia("Audio", "No hay AudioSource de musica asignado.", this);
            return;
        }

        musicSource.clip = musicList[musicIndex];
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlaySFX(int sfxIndex)
    {
        EnsureAudioSources();

        if (sfxList == null || sfxList.Length == 0 || sfxIndex < 0 || sfxIndex >= sfxList.Length)
        {
            return;
        }

        if (sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(sfxList[sfxIndex]);
    }

    private void EnsureMainMenuMusic()
    {
        EnsureAudioSources();

        if (!keepMainMenuMusicInAllScenes || musicSource == null || musicList == null || musicList.Length == 0)
        {
            return;
        }

        if (mainMenuMusicIndex < 0 || mainMenuMusicIndex >= musicList.Length)
        {
            GameDebug.Advertencia("Audio", $"mainMenuMusicIndex invalido: {mainMenuMusicIndex}. Se usa 0.", this);
            mainMenuMusicIndex = 0;
        }

        AudioClip targetClip = musicList[mainMenuMusicIndex];
        musicSource.volume = musicVolume;
        musicSource.loop = true;

        bool wrongClip = musicSource.clip != targetClip;
        bool stopped = !musicSource.isPlaying;

        if (wrongClip || stopped)
        {
            musicSource.clip = targetClip;
            musicSource.Play();
        }
    }

    private void EnsureAudioSources()
    {
        if (musicSource != null && sfxSource != null)
        {
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();

        if (musicSource == null && sources.Length > 0)
        {
            musicSource = sources[0];
        }

        if (sfxSource == null && sources.Length > 1)
        {
            sfxSource = sources[1];
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }
}
