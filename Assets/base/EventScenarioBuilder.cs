using System.Collections.Generic;
using UnityEngine;

public static class EventScenarioBuilder
{
    public static void GenerateRandomEvent(Node node, List<string> usedEventTitles)
    {
        node.nodeEvent = new EventData();

        switch (node.eventType)
        {
            case NodeEventType.Hostile:
            case NodeEventType.Elite:
                BuildCombatEvent(node);
                break;
            case NodeEventType.Boss:
                BuildBossEvent(node);
                break;
            case NodeEventType.Resource:
                SetupWeightedEvent(node, usedEventTitles,
                    new[] { "Pile of Bags", "Vending Machine", "Medical Cabinet", "Rusty Safe", "Abandoned Truck", "Military Crate" },
                    new[] { 35, 25, 20, 12, 5, 3 });
                break;
            case NodeEventType.Neutral:
                SetupWeightedEvent(node, usedEventTitles,
                    new[] { "Bulletin Board", "Abandoned Terminal", "Mysterious Merchant", "Rainwater Collector", "Underground Shelter", "Derailed Train" },
                    new[] { 40, 25, 15, 10, 7, 3 });
                break;
            case NodeEventType.Recruit:
                SetupWeightedEvent(node, usedEventTitles,
                    new[] { "Wandering Survivor", "Fallen Soldier", "Friendly Scavenger" },
                    new[] { 50, 30, 20 });
                break;
            default:
                node.nodeEvent.eventTitle = "SAFE";
                node.nodeEvent.eventDescription = "Safe place. Keep going?";
                break;
        }
    }

    private static void BuildCombatEvent(Node node)
    {
        bool isElite = node.eventType == NodeEventType.Elite;
        float mult = GetDifficultyMultiplier(node.difficulty);
        int reward = CalcReward(20, mult);

        node.nodeEvent.eventTitle = isElite ? "STRONG ENEMY" : "BATTLE";
        node.nodeEvent.eventDescription = isElite
            ? "A powerful enemy blocks the way! Reinforced armor and heavy weapons."
            : "Enemies spotted. Engage or evade?";

        EventChoice fight = new EventChoice { choiceText = "FIGHT!", triggersBattle = true };
        fight.outcomes.Add($"Victory! Enemy defeated. (Material+{reward})");
        node.nodeEvent.choices.Add(fight);

        int escapeFoodCost = isElite ? 10 : 5;
        EventChoice run = new EventChoice { choiceText = $"Evade (Lose {escapeFoodCost} Food)", foodReward = -escapeFoodCost };
        run.outcomes.Add($"Narrowly escaped. (Food-{escapeFoodCost})");
        node.nodeEvent.choices.Add(run);
    }

    private static void BuildBossEvent(Node node)
    {
        float mult = GetDifficultyMultiplier(node.difficulty);
        int reward = CalcReward(50, mult);

        node.nodeEvent.eventTitle = "BOSS BATTLE";
        node.nodeEvent.eventDescription = "The final gate. Prepare for the ultimate challenge!";

        EventChoice bossFight = new EventChoice { choiceText = "FINAL BATTLE!", triggersBattle = true };
        bossFight.outcomes.Add($"VICTORY! The fortress is yours. (Material+{reward})");
        node.nodeEvent.choices.Add(bossFight);
    }

    private static void SetupWeightedEvent(Node node, List<string> usedEventTitles, string[] titles, int[] weights)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < titles.Length; i++)
        {
            if (!usedEventTitles.Contains(titles[i]))
                available.Add(i);
        }

        if (available.Count == 0)
        {
            foreach (var t in titles) usedEventTitles.Remove(t);
            for (int i = 0; i < titles.Length; i++) available.Add(i);
        }

        int totalWeight = 0;
        foreach (int idx in available) totalWeight += weights[idx];

        int rand = Random.Range(0, totalWeight);
        int sum = 0;
        int selectedIdx = available[0];
        foreach (int idx in available)
        {
            sum += weights[idx];
            if (rand < sum) { selectedIdx = idx; break; }
        }

        string title = titles[selectedIdx];
        usedEventTitles.Add(title);
        node.nodeEvent.eventTitle = title;
        ConfigureScenario(node, title);
    }

    private static void ConfigureScenario(Node node, string title)
    {
        float mult = GetDifficultyMultiplier(node.difficulty);
        switch (node.eventType)
        {
            case NodeEventType.Resource: SetItemScenario(node, title, mult); break;
            case NodeEventType.Neutral: SetEventScenario(node, title, mult); break;
            case NodeEventType.Recruit: SetFriendScenario(node, title, mult); break;
        }
    }

    private static float GetDifficultyMultiplier(RegionDifficulty d)
    {
        switch (d)
        {
            case RegionDifficulty.Easy: return 1.0f;
            case RegionDifficulty.Normal: return 1.25f;
            case RegionDifficulty.Hard: return 1.6f;
            case RegionDifficulty.Extreme: return 2.2f;
            default: return 1.0f;
        }
    }

    private static int CalcReward(int baseValue, float mult)
    {
        return Mathf.RoundToInt(baseValue * mult);
    }

    private static void SetItemScenario(Node node, string title, float mult)
    {
        EventData ev = node.nodeEvent;
        switch (title)
        {
            case "Pile of Bags":
                ev.eventDescription = "Bags left behind by someone are scattered around.";
                int mat = CalcReward(15, mult);
                int foodB = CalcReward(5, mult);
                EventChoice bags = new EventChoice { choiceText = "Search", resourceReward = mat, foodReward = foodB, recordReward = 2 };
                bags.outcomes.Add($"Found useful items. (Material+{mat}, Food+{foodB}, Record+2)");
                if (Random.value < 0.2f) bags.outcomes.Add("Found a bandage. [HPRecover]");
                ev.choices.Add(bags);
                break;

            case "Vending Machine":
                ev.eventDescription = "An old vending machine with a faint power hum.";
                EventChoice vend = new EventChoice { choiceText = "Use (3 Material)", resourceReward = -3 };
                if (Random.value < 0.8f)
                {
                    int f = CalcReward(12, mult);
                    vend.foodReward = f;
                    string r = $"Machine works! (+{f} Food)";
                    if (Random.value < 0.3f) { vend.recordReward = 3; r += " Also found a magazine. (+3 Record)"; }
                    vend.outcomes.Add(r);
                }
                else vend.outcomes.Add("Machine rattles. Nothing came out. (Material-3)");
                ev.choices.Add(vend);
                ev.choices.Add(new EventChoice { choiceText = "Leave", outcomes = { "Nothing happened." } });
                break;

            case "Medical Cabinet":
                ev.eventDescription = "A white cabinet on the wall. Unlocked.";
                int cf = CalcReward(8, mult);
                EventChoice cab = new EventChoice { choiceText = "Open", foodReward = cf };
                cab.outcomes.Add($"Gathered medical supplies. (Food+{cf}) [HPRecover]");
                ev.choices.Add(cab);
                break;

            case "Rusty Safe":
                ev.eventDescription = "A rusty, heavy-looking safe. Password unknown.";
                int sm = CalcReward(30, mult);
                int sf = CalcReward(10, mult);
                EventChoice hack = new EventChoice { choiceText = "Crack (10 Record)", recordReward = -10, resourceReward = sm, foodReward = sf };
                hack.outcomes.Add($"Safe opened! (Material+{sm}, Food+{sf})");
                ev.choices.Add(hack);
                
                int km = CalcReward(15, mult);
                EventChoice kick = new EventChoice { choiceText = "Force Open (Risk)", resourceReward = km };
                kick.outcomes.Add($"Forced open. Damaged contents. (Material+{km})\n[CAUTION] Sprained foot. [HPDamage:10]");
                ev.choices.Add(kick);
                ev.choices.Add(new EventChoice { choiceText = "Leave", outcomes = { "You left the safe." } });
                break;

            case "Abandoned Truck":
                ev.eventDescription = "A supply truck with a full cargo bed.";
                int tm = CalcReward(40, mult);
                EventChoice truck = new EventChoice { choiceText = "Move Cargo (5 Food)", foodReward = -5, resourceReward = tm };
                string tr = $"Secured supplies! (+{tm} Material)";
                if (Random.value < 0.3f) tr += "\n[WARNING] Loud noise! [Noise]";
                truck.outcomes.Add(tr);
                ev.choices.Add(truck);
                ev.choices.Add(new EventChoice { choiceText = "Leave", outcomes = { "You left the truck." } });
                break;

            case "Military Crate":
                ev.eventDescription = "A military-sealed crate. Looks well-stocked.";
                int lcm = CalcReward(60, mult);
                int lcf = CalcReward(15, mult);
                int lcr = CalcReward(15, mult);
                EventChoice crate = new EventChoice { choiceText = "Take All", resourceReward = lcm, foodReward = lcf, recordReward = lcr };
                string cr = $"Jackpot! (Material+{lcm}, Food+{lcf}, Record+{lcr})";
                if (Random.value < 0.5f) cr += "\n[DANGER] Enemies alerted! [Noise]";
                crate.outcomes.Add(cr);
                ev.choices.Add(crate);
                ev.choices.Add(new EventChoice { choiceText = "Leave", outcomes = { "You gave up the supplies." } });
                break;
        }
    }

    private static void SetEventScenario(Node node, string title, float mult)
    {
        EventData ev = node.nodeEvent;
        switch (title)
        {
            case "Bulletin Board":
                ev.eventDescription = "Old posters messily covering a bulletin board.";
                int br = CalcReward(10, mult);
                EventChoice read = new EventChoice { choiceText = "Read", recordReward = br };
                read.outcomes.Add($"Useful information found. (Record+{br})");
                ev.choices.Add(read);
                break;

            case "Abandoned Terminal":
                ev.eventDescription = "Someone's personal terminal found on the street corner.";
                int tr = CalcReward(15, mult);
                EventChoice fix = new EventChoice { choiceText = "Restore (5 Food)", foodReward = -5, recordReward = tr };
                fix.outcomes.Add($"Data restored! (Record+{tr})");
                ev.choices.Add(fix);
                
                int pm = CalcReward(20, mult);
                EventChoice parts = new EventChoice { choiceText = "Strip Parts", resourceReward = pm };
                parts.outcomes.Add($"Dismantled for parts. (Material+{pm})");
                ev.choices.Add(parts);
                break;

            case "Mysterious Merchant":
                ev.eventDescription = "A gas-masked merchant eyes your supplies.";
                int mtr = CalcReward(15, mult);
                ev.choices.Add(new EventChoice { choiceText = "Food → Data", foodReward = -10, recordReward = mtr,
                    outcomes = { $"Deal done. (Record+{mtr}, Food-10)" } });
                    
                int mtm = CalcReward(25, mult);
                ev.choices.Add(new EventChoice { choiceText = "Data → Material", recordReward = -5, resourceReward = mtm,
                    outcomes = { $"Deal done. (Material+{mtm}, Record-5)" } });
                ev.choices.Add(new EventChoice { choiceText = "Refuse", outcomes = { "Merchant shrugs and walks off." } });
                break;

            case "Rainwater Collector":
                ev.eventDescription = "An old water tank. Relatively clean water inside.";
                int rf = CalcReward(8, mult);
                EventChoice water = new EventChoice { choiceText = "Collect Water", foodReward = rf };
                water.outcomes.Add(Random.value < 0.1f
                    ? $"Device collapsed! (Food+{rf})\n[INJURY] [HPDamage:10]"
                    : $"Clean water collected. (Food+{rf})");
                ev.choices.Add(water);
                break;

            case "Underground Shelter":
                ev.eventDescription = "A steel door leads to an underground shelter.";
                int sFd = CalcReward(15, mult);
                EventChoice search = new EventChoice { choiceText = "Search (5 Material)", resourceReward = -5, foodReward = sFd };
                string shR = $"Food storage found! (Food+{sFd})";
                if (Random.value < 0.2f) shR += "\n[ACCIDENT] Structure collapsed! [HPDamage:10]";
                search.outcomes.Add(shR);
                ev.choices.Add(search);
                ev.choices.Add(new EventChoice { choiceText = "Rest (5 Food)", foodReward = -5,
                    outcomes = { "Felt refreshed. [HPRecover]" } });
                break;

            case "Derailed Train":
                ev.eventDescription = "A massive train car lying off its tracks.";
                int trm = CalcReward(50, mult);
                EventChoice train = new EventChoice { choiceText = "Inspect (5 Food)", foodReward = -5, resourceReward = trm };
                string tRes = $"Massive haul! (Material+{trm})";
                if (Random.value < 0.5f) tRes += "\n[DANGER] Loud noise! [Noise]";
                train.outcomes.Add(tRes);
                ev.choices.Add(train);
                break;
        }
    }

    private static void SetFriendScenario(Node node, string title, float mult)
    {
        EventData ev = node.nodeEvent;
        switch (title)
        {
            case "Wandering Survivor":
                ev.eventDescription = "A survivor wandering aimlessly in the wasteland.";
                int r = CalcReward(30, mult);
                ev.choices.Add(new EventChoice { choiceText = "Share Food (10 Food)", foodReward = -10, recordReward = r,
                    outcomes = { $"Survivor shares intel. (Record+{r}, Food-10)" } });
                ev.choices.Add(new EventChoice { choiceText = "Pass by",
                    outcomes = { "You walked past the desperate survivor." } });
                break;

            case "Fallen Soldier":
                ev.eventDescription = "A soldier's body lying in a cold alley.";
                int dr = CalcReward(15, mult);
                ev.choices.Add(new EventChoice { choiceText = "Collect Dogtag", recordReward = dr,
                    outcomes = { $"Dogtag secured. (Record+{dr})" } });
                int dm = CalcReward(25, mult);
                ev.choices.Add(new EventChoice { choiceText = "Search Gear", resourceReward = dm,
                    outcomes = { $"Gear found. (Material+{dm})" } });
                break;

            case "Friendly Scavenger":
                ev.eventDescription = "A cheerful scavenger rummaging through ruins.";
                int sF = CalcReward(30, mult);
                ev.choices.Add(new EventChoice { choiceText = "Barter (10 Material)", resourceReward = -10, foodReward = sF,
                    outcomes = { $"Fair trade! (Food+{sF}, Material-10)\n[Recruit:Scavenger:15]" } });
                ev.choices.Add(new EventChoice { choiceText = "Wave",
                    outcomes = { "The scavenger waves back cheerfully." } });
                break;
        }
    }
}
