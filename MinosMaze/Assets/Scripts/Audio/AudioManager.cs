using UnityEngine;
using DG.Tweening;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<AudioManager>();
            return _instance;
        }
    }

    [SerializeField] private AudioConfig config;

    public AudioConfig Config => config;

    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource sfxSource;
    private bool usingSourceA = true;
    private bool _sourcesInitialized;

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;

    private AudioSource ActiveBGMSource => usingSourceA ? bgmSourceA : bgmSourceB;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticFields()
    {
        _instance = null;
    }

    private void InitializeSources()
    {
        if (_sourcesInitialized) return;
        _sourcesInitialized = true;

        bgmSourceA = gameObject.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceA.playOnAwake = false;
        bgmSourceA.volume = 0f;

        bgmSourceB = gameObject.AddComponent<AudioSource>();
        bgmSourceB.loop = true;
        bgmSourceB.playOnAwake = false;
        bgmSourceB.volume = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
    {
        InitializeSources();

        if (clip == null)
        {
            StopBGM(fadeDuration);
            return;
        }

        if (ActiveBGMSource.clip == clip && ActiveBGMSource.isPlaying)
            return;

        AudioSource oldSource = ActiveBGMSource;
        oldSource.DOKill();
        oldSource.DOFade(0f, fadeDuration).OnComplete(() => oldSource.Stop());

        usingSourceA = !usingSourceA;

        AudioSource newSource = ActiveBGMSource;
        newSource.clip = clip;
        newSource.volume = 0f;
        newSource.Play();
        newSource.DOKill();
        newSource.DOFade(bgmVolume, fadeDuration);
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        InitializeSources();

        bgmSourceA.DOKill();
        bgmSourceB.DOKill();

        if (bgmSourceA.isPlaying)
            bgmSourceA.DOFade(0f, fadeDuration).OnComplete(() => bgmSourceA.Stop());
        if (bgmSourceB.isPlaying)
            bgmSourceB.DOFade(0f, fadeDuration).OnComplete(() => bgmSourceB.Stop());
    }

    public void PlaySFX(AudioClip clip)
    {
        InitializeSources();

        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFX(AudioClip clip, float pitchVariation)
    {
        InitializeSources();

        if (clip == null) return;
        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, sfxVolume);
        sfxSource.pitch = originalPitch;
    }

    public void SetBGMVolume(float volume)
    {
        InitializeSources();

        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSourceA.isPlaying) bgmSourceA.volume = bgmVolume;
        if (bgmSourceB.isPlaying) bgmSourceB.volume = bgmVolume;
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
    }

    public void SetSFXVolume(float volume)
    {
        InitializeSources();

        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void PlayBGMForScene(string sceneName, MapCellType cellType = MapCellType.Battle_Empty)
    {
        InitializeSources();

        if (config == null) return;

        switch (sceneName)
        {
            case "1_MainMenu":
            case "1_Mainmenu":
                PlayBGM(config.mainMenuBGM);
                break;
            case "2.0_WorldMap":
                PlayBGM(config.worldMapBGM);
                break;
            case "2.1_BattleScene":
                if (cellType == MapCellType.WorldMap_Elite || cellType == MapCellType.WorldMap_Boss)
                    PlayBGM(config.eliteBattleBGM);
                else
                {
                    AudioClip clip = config.normalBattleBGMs != null && config.normalBattleBGMs.Length > 0
                        ? config.normalBattleBGMs[Random.Range(0, config.normalBattleBGMs.Length)]
                        : null;
                    PlayBGM(clip);
                }
                break;
            case "2.2_RestSite":
                PlayBGM(config.restSiteBGM);
                break;
            case "2.3_StatueScene":
            case "2.4_Treasure":
                StopBGM();
                break;
            default:
                StopBGM();
                break;
        }
    }
}
