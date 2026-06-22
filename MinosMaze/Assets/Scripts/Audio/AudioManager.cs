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

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

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

    public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
    {
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
        bgmSourceA.DOKill();
        bgmSourceB.DOKill();

        if (bgmSourceA.isPlaying)
            bgmSourceA.DOFade(0f, fadeDuration).OnComplete(() => bgmSourceA.Stop());
        if (bgmSourceB.isPlaying)
            bgmSourceB.DOFade(0f, fadeDuration).OnComplete(() => bgmSourceB.Stop());
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFX(AudioClip clip, float pitchVariation)
    {
        if (clip == null || sfxSource == null) return;
        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, sfxVolume);
        sfxSource.pitch = originalPitch;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (ActiveBGMSource.isPlaying)
            ActiveBGMSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void PlayBGMForScene(string sceneName, MapCellType cellType = MapCellType.Battle_Empty)
    {
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
