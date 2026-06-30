using System;

[Serializable]
public class LoreData
{
    public string id;
    public string title;
    [UnityEngine.TextArea(3, 10)]
    public string content;
    public string originCorp;

    public LoreData(string id, string title, string content, string originCorp = "")
    {
        this.id = id;
        this.title = title;
        this.content = content;
        this.originCorp = originCorp;
    }
}
