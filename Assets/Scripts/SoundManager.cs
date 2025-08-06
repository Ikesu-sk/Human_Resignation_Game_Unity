using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource bgmAudiioSorce = default;
    [SerializeField] private AudioClip[] bgmClips;

    public enum BGM
    {
        test
    }

    public void PlayBGM(BGM bgm)
    {
        if (bgmAudiioSorce.clip != bgmClips[(int)bgm])
        {
            bgmAudiioSorce.clip = bgmClips[(int)bgm];
            bgmAudiioSorce.Play();
        }
    }
}
