
using UnityEngine;
using System;

public class ItemChest : MonoBehaviour, SpawnedObject
{
    public int numFirstAidKit { get; private set; }
    public int numAmmo { get; private set; }

    public void Init(ObjectSpawner spawner)
    {
        numFirstAidKit = 0;
        numAmmo = 0;

        string containItems = spawner.RunRule('I');
        foreach (char c in containItems)
        {
            if (c == 'F')
                numFirstAidKit = int.Parse(spawner.RunRule('F'));
            else if (c == 'A')
                numAmmo = int.Parse(spawner.RunRule('A'));
        }
    }
}
