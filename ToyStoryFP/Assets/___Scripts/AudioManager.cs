using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clip Arrays")]
    public AudioClip[] musicList;
    public AudioClip[] sfxList;

    [Header("Audio Source References")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicList.Length > 0)
        {
            musicSource.volume = 0.05f;
            PlayMusic(0);
        }
    }
    
    public void PlayMusic(int musicIndex)
    {
        musicSource.clip = musicList[musicIndex];
        musicSource.Play();
    }

    public void PlaySFX(int sfxIndex)
    {
        sfxSource.PlayOneShot(sfxList[sfxIndex]);
    }
}
