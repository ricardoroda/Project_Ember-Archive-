using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSO", menuName = "Scriptable Objects/ProjectileSO")]
public class ProjectileSO : AbilitySO
{
    [Header("Projectile Number Attributes")]
    public int numberOfProjectiles = 1;
    public float spreadBetweenProjectiles = 0;
    public int burst = 1;
    public float distanceBetweenBurst = 0;

    [Header("Movement Attributes")]
    public float baseSpeed = 10;
    public bool penetrate = false;
    public bool homming = false;
    public float hommingRadius = 5.0f;
    public float hommingTurnRate = 0;
    public bool returning = false;
    public float returningTime = 0;

    [Header("Projectile Size Attributes")]
    public float projectileScaleIncrease = 1;
}
