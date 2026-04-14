using UnityEngine;

[CreateAssetMenu(fileName = "DashSO", menuName = "Scriptable Objects/DashSO")]
public class DashSO : AbilitySO
{
    [Header("Dash Attributes")]
    public float dashForce;
}
