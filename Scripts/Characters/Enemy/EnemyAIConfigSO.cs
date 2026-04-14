using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy AI Config", fileName = "EnemyAiConfigSo")]
public class EnemyAiConfigSo : ScriptableObject
{
    [Header("Detection")]
    [Tooltip("How far the enemy can spot the player. 0 = use EnemyStats or per-instance override.")]
    public float detectionRadius = 10f;

    [Tooltip("Field of view in degrees (360 = see all around).")]
    [Range(0f, 360f)]
    public float fieldOfView = 360f;

    [Tooltip("How long the enemy remembers the player's last seen spot (seconds).")]
    public float memoryDuration = 3f;

    [Tooltip("Eye height for line-of-sight checks.")]
    public float eyeHeight = 1f;

    [Header("Obstacles")]
    [Tooltip("Layers that block vision.")]
    public LayerMask obstacleMask = Physics.DefaultRaycastLayers;

    [Header("Boss Settings")]
    [Tooltip("How much bigger a boss's detection radius should be.")]
    public float bossDetectionMultiplier = 1.5f;

    [Header("Patrol Settings")]
    [Tooltip("Enable basic patrol while idle.")]
    public bool patrolEnabled;

    [Tooltip("Radius around spawn to pick patrol points.")]
    public float patrolRadius = 5f;

    [Tooltip("How many random patrol points to make.")]
    public int patrolPointCount = 4;

    [Tooltip("Seconds to wait at each patrol point.")]
    public float patrolWaitTime = 1.2f;

    [Tooltip("Speed multiplier while patrolling.")]
    public float patrolSpeedMultiplier = 1f;

    [Header("Aggression Settings")]
    [Tooltip("Speed multiplier while chasing.")]
    public float chaseSpeedMultiplier = 1.2f;

    [Tooltip("If true, ranged AI will kite instead of rush in.")]
    public bool rangedKitingEnabled;

    [Tooltip("Force this AI to be treated as ranged.")]
    public bool forceRanged;

    [Tooltip("Preferred distance for kiting AI.")]
    public float preferredKitingDistance = 6f;

    [Tooltip("Speed multiplier while kiting.")]
    public float kiteSpeedMultiplier = 1.0f;

    // Add more tweakables here as needed.
}
