using UnityEngine;

/// <summary>
/// Moves a Rigidbody smoothly for a short dash using MovePosition in FixedUpdate.
/// This avoids abruptly overwriting vertical velocity and reduces solver popping.
/// Now performs a sweep test to stop before penetrating geometry.
/// </summary>
[DisallowMultipleComponent]
public class DashMover : MonoBehaviour
{
    private Rigidbody _rb;
    private Vector3 _direction;
    private float _speed;
    private float _timeLeft;
    // small safety margin to avoid ending exactly inside colliders
    private const float SkinWidth = 0.02f;

    public void Init(Rigidbody rb, Vector3 direction, float speed, float duration)
    {
        _rb = rb;
        _direction = direction.normalized;
        _speed = speed;
        _timeLeft = duration;
    }

    void FixedUpdate()
    {
        if (_rb == null) { Destroy(this); return; }

        if (_timeLeft <= 0f)
        {
            Destroy(this);
            return;
        }

        // desired horizontal move for this physics step
        Vector3 move = _direction * (_speed * Time.fixedDeltaTime);
        float dist = move.magnitude;
        if (dist <= 1e-6f)
        {
            _timeLeft -= Time.fixedDeltaTime;
            return;
        }

        // Perform a sweep test using the Rigidbody to detect upcoming collisions along the dash direction.
        // If we would hit something, move to the hit point minus a small skin width and stop the dash.
        if (_rb.SweepTest(_direction, out RaycastHit hit, dist + SkinWidth))
        {
            // stop before we intersect the collider
            float moveDist = Mathf.Max(0f, hit.distance - SkinWidth);
            Vector3 target = _rb.position + _direction * moveDist;
            // preserve vertical position (don't force Y)
            target.y = _rb.position.y;

            _rb.MovePosition(target);

            // end dash early to avoid solver popping
            Destroy(this);
            return;
        }

        // no hit: move normally, preserving vertical position
        Vector3 targetPos = _rb.position + move;
        targetPos.y = _rb.position.y; // don't change Y
        _rb.MovePosition(targetPos);

        _timeLeft -= Time.fixedDeltaTime;
    }
}
