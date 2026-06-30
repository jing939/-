using System;
using System.Collections.Generic;

[Serializable]
public class AllyData
{
    public string name;
    public int power;
    public string originCorp; // 출신 사 (B, M, K...)
    public List<string> tags = new List<string>(); // "Combat", "Support" 등 추후 확장성

    public AllyData(string name, int power, string originCorp = "")
    {
        this.name = name;
        this.power = power;
        this.originCorp = originCorp;
    }
}
