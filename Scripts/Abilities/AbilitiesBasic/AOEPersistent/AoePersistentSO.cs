using UnityEngine;

[CreateAssetMenu(fileName = "AoePersistentSO", menuName = "Scriptable Objects/AoePersistentSO")]
public class AoePersistentSO : AbilitySO
{
    [Header("AOEP Type")]
    public bool self = false;
    public bool delayedAOE = false;
    public float delayTime = 0f;

    [Header("AOEP Attributes")]
    public float radius = 0;
    public float startingRadiusRelative = 0.5f;

    [Header("AOEP Effect/Timers")]
    public float tickTime = 0f;

    [Header("Triggered")]
    public bool triggeredAttach = false;

}
