using System;
using System.Collections.Generic;
using UnityEngine;

// 会話のセリフや画像素材などを整理する設計図
[CreateAssetMenu(fileName = "New Data", menuName = "StoryData")]
public class StoryData : ScriptableObject
{
    // storiesという名前のリストを作成
    public List<Story> stories = new List<Story>();
    public SoundManager.BGM bgm;
}

[System.Serializable]
public class Story
{
    public Sprite Background;
    public Sprite CharacterImage;
    [TextArea]
    public String StoryText;
    public string CharacterName;
}