using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottleFlipDecision : MonoBehaviour, Decision
{
    public float Action;

    public float[] Decide(List<float> state, List<Camera> observation, float reward, bool done, float[] memory)
    {
        float[] action = new float[1];
        action[0] = Action; //Random.Range(0, 2f);
        return action;
    }

    public float[] MakeMemory(List<float> state, List<Camera> observation, float reward, bool done, float[] memory)
    {
        return default(float[]);
    }
}