using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EventChoice
{
    public string choiceText;
    public int resourceReward;     
    public int foodReward;         // Added for consistency
    public int recordReward;       // Added for consistency
    public int techFragmentReward; 
    public bool triggersBattle;    
    public List<string> outcomes = new List<string>(); // Possible results
}

[Serializable]
public class EventData
{
    public string eventTitle;
    [TextArea(3, 10)]
    public string eventDescription;
    public List<EventChoice> choices = new List<EventChoice>();
}
