using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yarn.Unity;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private Coroutine musicFadeCoroutine;
    private Coroutine uiBeepCoroutine;

    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;

    private float _musicVolume = 1f;
    public float musicVolume {
        get { return _musicVolume; }
        set { _musicVolume = value; musicSource.volume = value; }
    }

    private float _sfxVolume = 1f;
    public float sfxVolume {
        get { return _sfxVolume; }
        set { _sfxVolume = value; sfxSource.volume = value; }
    }

    private float _uiVolume = 1f;
    public float uiVolume {
        get { return _uiVolume; }
        set { _uiVolume = value; uiSource.volume = value; }
    }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    
    [YarnCommand]
    public static void PlayMusic(string musicName)
    {
        AudioManager.Instance.PlayMusicInternal(musicName, 1f, 1f, 0f);
    }


    [YarnCommand]
    public static void FadeInMusic(string musicName, float fadeDuration)
    {
        AudioManager.Instance.PlayMusicInternal(musicName, 0f, 1f, fadeDuration);
    }


    [YarnCommand]
    public static void FadeOutMusic(string musicName, float fadeDuration)
    {
        AudioManager.Instance.PlayMusicInternal(musicName, 1f, 0f, fadeDuration);
    }


    [YarnCommand]
    public static void PlaySFX(string sfxName)
    {
        AudioManager.Instance.PlaySFXInternal(sfxName);
    }


    public void StartDialogueBeeping(params AudioClip[] beeps)
    {
        StopDialogueBeeping();
        uiBeepCoroutine = StartCoroutine(DialogueBeepCoroutine(beeps));
    }


    public void StopDialogueBeeping()
    {
        if (uiBeepCoroutine != null)
        {
            StopCoroutine(uiBeepCoroutine);
            uiBeepCoroutine = null;
        }
    }


    private void PlayMusicInternal(string musicName, float fromVol, float toVol, float fadeDuration)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        // Just play music, no coroutine needed
        if (fromVol == toVol || fadeDuration <= 0f)
        {
            musicVolume = toVol;
            LoadAndPlayMusic(musicName);
        }
        else
        {
            musicFadeCoroutine = StartCoroutine(FadeMusicCoroutine(musicName, fromVol, toVol, fadeDuration));
        }
    }


    private IEnumerator FadeMusicCoroutine(string musicName, float fromVol, float toVol, float fadeDuration)
    {
        musicVolume = fromVol;
        LoadAndPlayMusic(musicName);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            float volScale = Mathf.Lerp(fromVol, toVol, timer / fadeDuration);
            musicVolume = volScale;

            timer += Time.deltaTime;
            yield return new WaitForSeconds(0f);
        }

        musicVolume = toVol;
        musicFadeCoroutine = null;
    }


    private IEnumerator DialogueBeepCoroutine(AudioClip[] beeps)
    {
        // Keep picking and playing beeps at random until the coroutine is stopped.
        // A very slight variance in delay is added cuz it sounds good
        while (true)
        {
            AudioClip beep = beeps[Random.Range(0, beeps.Length)];
            float beepLength = beep.length;
            uiSource.PlayOneShot(beep);
            yield return new WaitForSeconds(beepLength + Random.Range(-beepLength * 0.15f, 0f));
        }
    }


    private void PlaySFXInternal(string sfxName)
    {
        string sfxPath = Path.Combine(Application.dataPath, "Audio", "SFX", sfxName);
        AudioClip sfxClip = LoadAudioFromFile(sfxPath);
        sfxSource.PlayOneShot(sfxClip);
    }


    private void LoadAndPlayMusic(string musicName)
    {
        string musicPath = Path.Combine(Application.dataPath, "Audio", "Music", musicName);
        AudioClip musicClip = LoadAudioFromFile(musicPath);
        musicSource.clip = musicClip;
        musicSource.Play();
    }


    private AudioClip LoadAudioFromFile(string audioFilePath)
    {
        // TODO get path WITHOUT exension, then try all supported extensions in order: MP3, WAV, OGG, AIFF/AIF
        return null;
    }
}
