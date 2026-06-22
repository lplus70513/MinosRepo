using UnityEngine;

[CreateAssetMenu(menuName = "MinosMaze/Audio Config")]
public class AudioConfig : ScriptableObject
{
    [Header("BGM")]
    public AudioClip mainMenuBGM;
    public AudioClip worldMapBGM;
    public AudioClip[] normalBattleBGMs;
    public AudioClip eliteBattleBGM;
    public AudioClip restSiteBGM;

    [Header("战斗音效")]
    public AudioClip playerAttackSFX;
    public AudioClip playerHitSFX;

    [Header("交互音效")]
    public AudioClip buttonClickSFX;
    public AudioClip goldCollectSFX;
    public AudioClip stringCollectSFX;
    public AudioClip cardDeleteSFX;
    public AudioClip cardUpgradeSFX;
    public AudioClip blessingSFX;
    public AudioClip playerMoveSFX;
    public AudioClip healSFX;
}
