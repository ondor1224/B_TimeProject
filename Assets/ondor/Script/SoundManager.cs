using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I;
    public static bool SfxOn = true, BgmOn = true;
    [Range(0, 1)] public float sfxVol = 1, bgmVol = .5f;
    AudioSource bgm, sfx;

    void Awake()
    {
        if (I) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        SfxOn = PlayerPrefs.GetInt("sfx", 1) == 1;
        BgmOn = PlayerPrefs.GetInt("bgm", 1) == 1;
        bgm = gameObject.AddComponent<AudioSource>();
        sfx = gameObject.AddComponent<AudioSource>();
        bgm.loop = true;
        bgm.playOnAwake = sfx.playOnAwake = false;
        bgm.volume = bgmVol;
        bgm.mute = !BgmOn;
    }

    public static void PlaySfx(AudioClip c)
    {
        if (I && SfxOn && c) I.sfx.PlayOneShot(c, I.sfxVol);
    }

    public static void PlayBgm(AudioClip c)
    {
        if (!I || !c || I.bgm.clip == c) return;
        I.bgm.clip = c;
        I.bgm.Play();
    }

    public static void Set(bool isBgm, bool on)
    {
        if (isBgm) { BgmOn = on; if (I) I.bgm.mute = !on; }
        else SfxOn = on;
        PlayerPrefs.SetInt(isBgm ? "bgm" : "sfx", on ? 1 : 0);
    }
}
