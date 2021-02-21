using System.Collections.Generic;
using System.Collections.ObjectModel;

public class SpawningGrammar
{
    struct Rule
    {
        public string successor { get; set; }
        public float weight { get; set; }

        public Rule(string suc, float weight)
        {
            this.successor = suc;
            this.weight = weight;
        }
    }

    private static ReadOnlyDictionary<char, Rule[]> s_kReproducingRules = new ReadOnlyDictionary<char, Rule[]>(new Dictionary<char, Rule[]>
    {
        // Start
        {
            'S', 
            new Rule[]
            {
                new Rule("EEEEEEEEEEEEEEEEEEEE", 0.25f),
                new Rule("EEECEEECEEECEEECEEECEE", 0.25f),
                new Rule("EECEECEECEECEECEECECEEEE", 0.25f),
                new Rule("CEEEEEEEEECEEEEEEEEE", 0.25f),
            }
        },

        // Enemy
        {
            'E', 
            new Rule[]
            {
                new Rule("EC", 0.3f),
                new Rule("EE", 0.1f),
                new Rule("EI", 0.1f),
                new Rule("E",  0.4f),
                new Rule("_",  0.1f),
            }
        },

        // Summoning Magic Circle
        {
            'C', 
            new Rule[]
            {
                new Rule("E",  0.35f),
                new Rule("I",  0.25f),
                new Rule("EE", 0.2f),
                new Rule("_",  0.2f),
            }
        },
    });

    private static ReadOnlyDictionary<char, Rule[]> s_kResultRules = new ReadOnlyDictionary<char, Rule[]>(new Dictionary<char, Rule[]>
    {
        // Enemy reward
        {
            'E', 
            new Rule[]
            {
                new Rule("I",       0.7f),
                new Rule("_",       0.3f),
            }
        },

        // Summoning Magic Circle
        {
            'C', 
            new Rule[]
            {
                new Rule("EEEEE",   0.1f),
                new Rule("II",      0.3f),
                new Rule("I",       0.3f),
                new Rule("E",       0.2f),
                new Rule("_",       0.1f),
            }
        },

        // Item Container
        {
            'I', 
            new Rule[]
            {
                new Rule("FA", 0.25f),
                new Rule("A",  0.5f),
                new Rule("AA",  0.25f),
            }
        },

        // First-aid
        {
            'F', 
            new Rule[]
            {
                new Rule("1", 0.6f),
                new Rule("2", 0.3f),
                new Rule("3", 0.1f),
            }
        },

        // Ammo
        {
            'A', 
            new Rule[]
            {
                new Rule("10", 0.3f),
                new Rule("20", 0.4f),
                new Rule("30", 0.2f),
                new Rule("40", 0.1f),
            }
        },
    });

    private static ReadOnlyDictionary<char, string> s_kDescription = new ReadOnlyDictionary<char, string>(new Dictionary<char, string>
    {
        { 'E', "Enemy" },
        { 'C', "Summoning Magic Circle" },
        { 'I', "Item Chest" },
        { 'F', "First-aid Kit" },
        { 'A', "Ammo" },
    });

    private static ReadOnlyCollection<char> s_kTerminators = new ReadOnlyCollection<char> (new char[] 
    {
        'I', 'A', 'F', '_'
    });

    public static string GetDescription(char c)
    {
        if (!s_kDescription.TryGetValue(c, out string res))
            throw new System.InvalidOperationException("Can't find rules for the character");

        return res;
    }

    public static bool IsTerminator(char c)
    {
        return s_kTerminators.Contains(c);
    }

    public string InitialGeneration(XOrShiftRNG rng)
    {
        return RunReproducingRule('S', rng);
    }

    public string CalculateNextFormation(string current, XOrShiftRNG rng)
    {
        string newFormation = string.Empty;

        for (int i = 0; i < current.Length; ++i)
        {
            newFormation += RunReproducingRule(current[i], rng);
        }

        return newFormation;
    }

    public string RunReproducingRule(char c, XOrShiftRNG rng)
    {
        if (IsTerminator(c))
            return string.Empty;
        
        if (!s_kReproducingRules.TryGetValue(c, out var rules))
            throw new System.InvalidOperationException("Can't find rules for the character");
        
        float totalWeight = 0.0f;
        for (int i = 0; i < rules.Length; ++i)
        {
            totalWeight += rules[i].weight;
        }

        float choice = rng.RandomFloatRange(0f, 1f) * totalWeight;
        for (int i = 0; i < rules.Length; ++i)
        {
            choice -= rules[i].weight;
            if (choice <= 0)
                return rules[i].successor;
        }

        throw new System.InvalidOperationException("Failed to get a valid choice from grammar rules");
    }

    public string RunResultRule(char c, XOrShiftRNG rng)
    {      
        if (c == '_')
            return string.Empty;

        if (!s_kResultRules.TryGetValue(c, out var rules))
            throw new System.InvalidOperationException("Can't find rules for the character");
        
        float totalWeight = 0.0f;
        for (int i = 0; i < rules.Length; ++i)
        {
            totalWeight += rules[i].weight;
        }

        float choice = rng.RandomFloatRange(0f, 1f) * totalWeight;
        for (int i = 0; i < rules.Length; ++i)
        {
            choice -= rules[i].weight;
            if (choice <= 0)
                return rules[i].successor;
        }

        throw new System.InvalidOperationException("Failed to get a valid choice from grammar rules");
    }
}
