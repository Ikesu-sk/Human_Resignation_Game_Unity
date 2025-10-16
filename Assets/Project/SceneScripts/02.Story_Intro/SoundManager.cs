using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource bgmAudiioSorce = default;
    [SerializeField] private AudioClip[] bgmClips;

    // BGMの種類を名前で選べるようにする。
    public enum BGM
    {
        yume, // 配列の0番目の曲　名前はなんでもよい
        Electronic_Violence, // ２番目
        GameOver // ゲームオーバー専用BGM
    }

    public void PlayBGM(BGM bgm)
    {
        if (bgmAudiioSorce.clip != bgmClips[(int)bgm])
        {
            bgmAudiioSorce.clip = bgmClips[(int)bgm];
            bgmAudiioSorce.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmAudiioSorce.isPlaying)
        {
            bgmAudiioSorce.Stop();
        }
    }
    
}
