using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using UnityEngine;
using System.Text;

public enum MissionType
{
    kReconnaissance,
    kBattle,
}

public enum MissionLevel
{
    kMajor,
    kMinor
}

public struct MissionInfo
{
    public MissionType type { get; set; }
    public MissionLevel level { get; set; }
    public TargetType target { get; set; }
    public string description { get; set; }
}

public class LSystemMissionGenerator
{
    private static ReadOnlyDictionary<char, string> s_kRules = new ReadOnlyDictionary<char, string>( new Dictionary<char, string> {
        { 'A', "AB" },
        { 'B', "BA" }
    });
    
    private static ReadOnlyCollection<MissionInfo> s_kMissions = new ReadOnlyCollection<MissionInfo>(new MissionInfo[] {
        
        new MissionInfo{ 
            type = MissionType.kBattle, 
            target = TargetType.kEnemy,
            description = "Eliminate space invader."
        },

        new MissionInfo{ 
            type = MissionType.kBattle, 
            target = TargetType.kMagicCircle,
            description = "Destroy summoning magic circles."
        },

        new MissionInfo{ 
            type = MissionType.kReconnaissance, 
            description = "Head over to target location and investigate the operational environment." 
        },  

    });

    private static int s_kMaxIteration = 10;

    private int m_numberOfMissions;
    private string m_currentString;
    private string m_axiom;

    public LSystemMissionGenerator(int numberOfMissions)
    {
        m_axiom = "A";
        m_currentString = string.Empty;
        m_numberOfMissions = numberOfMissions;
    }

    public void GenerateMissionInfo(XOrShiftRNG rng, ConcurrentQueue<MissionInfo> missions)
    {
        GenerateRawMissionString();
        
        int cutStart = rng.RandomIntRange(0, m_currentString.Length - m_numberOfMissions);
        string missionStr = m_currentString.Substring(cutStart, m_numberOfMissions);

        Debug.Log("Mission start: " + cutStart.ToString());
        Debug.Log("Mission String: " + missionStr);

        foreach (char c in missionStr)
        {
            MissionInfo mission = GenerateMission(rng);

            if (c == 'A')
                mission.level = MissionLevel.kMajor;
            else if (c == 'B')
                mission.level = MissionLevel.kMinor;

            missions.Enqueue(mission);
        }
    }

    private void GenerateRawMissionString()
    {
        m_currentString = m_axiom;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s_kMaxIteration; ++i)
        {
            foreach (char c in m_currentString)
            {
                sb.Append(s_kRules[c]);
            }

            m_currentString = sb.ToString();
            sb = new StringBuilder();
        }
    }

    private MissionInfo GenerateMission(XOrShiftRNG rng)
    {
        int index = rng.RandomIntRange(0, s_kMissions.Count);
        Debug.Log("Mission Count: " + s_kMissions.Count.ToString() + ", mission index: " + index.ToString());

        MissionInfo mission = s_kMissions[index];
        return mission;
    }
}
