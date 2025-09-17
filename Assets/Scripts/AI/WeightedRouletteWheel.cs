using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedOption<T>
{
    public T option;
    public float weight;
    
    public WeightedOption(T option, float weight)
    {
        this.option = option;
        this.weight = weight;
    }
}

public static class WeightedRouletteWheel
{
    public static T SelectOption<T>(List<WeightedOption<T>> options)
    {
        if (options == null || options.Count == 0)
            return default(T);
        
        float totalWeight = 0f;
        foreach (var option in options)
        {
            totalWeight += option.weight;
        }
        
        if (totalWeight <= 0f)
            return options[0].option;
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var option in options)
        {
            currentWeight += option.weight;
            if (randomValue <= currentWeight)
            {
                return option.option;
            }
        }
        
        return options[options.Count - 1].option;
    }
}

public enum LeaderDecision
{
    AttackAggressive,
    AttackCautious,
    Retreat
}

public enum TacticalAction
{
    Fortify,
    Heal,
    Continue
}
