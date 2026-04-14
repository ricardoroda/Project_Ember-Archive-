using UnityEngine;

[CreateAssetMenu(fileName = "AoeExplosionSO", menuName = "Scriptable Objects/AoeExplosionSO")]
public class AoeExplosionSO : AbilitySO
{
    [Header("AOE Type")]
    public bool centerSelf = false;
    public bool delayedAOE = false;
    public float delayTime = 0f;

    [Header("AOE Attributes")]
    public float radius = 0;
    public float startingRadiusRelative = 0.5f;

    [Header("Multiple AOE Attributes")]
    public int repetitionCount = 1;
    public float repetitionDelay = 0;
    public float repetitionScaleIncrease = 0;
}
