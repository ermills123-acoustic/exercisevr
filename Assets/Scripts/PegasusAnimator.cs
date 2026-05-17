using UnityEngine;

public class PegasusAnimator : MonoBehaviour
{
    [Header("Wing References")]
    public Transform leftWing;
    public Transform rightWing;

    [Header("Flap Animation Settings")]
    public float baseFlapSpeed = 5.0f;
    public float maxFlapAngle = 25.0f;
    
    private VRMotionController motionController;

    void Start()
    {
        // Locate the VRMotionController on the parent or nearby object
        motionController = GetComponentInParent<VRMotionController>();
        if (motionController == null)
        {
            motionController = FindFirstObjectByType<VRMotionController>();
        }
    }

    void Update()
    {
        if (leftWing == null || rightWing == null) return;

        // Determine animation speed based on flight state
        float speedMultiplier = 1.0f;
        
        if (motionController != null)
        {
            if (motionController.IsCurrentlyFlying())
            {
                // Flap faster when moving forward
                speedMultiplier = 2.5f + (motionController.GetCurrentSpeed() / motionController.forwardSpeed) * 1.5f;
            }
            else
            {
                // Slow idle flutter when stationary
                speedMultiplier = 0.5f;
            }
        }

        // Generate flapping angle using a sine wave
        float angle = Mathf.Sin(Time.time * baseFlapSpeed * speedMultiplier) * maxFlapAngle;

        // Apply symmetric rotations to the wings
        // Assuming the wings are oriented such that Z-axis or X-axis rotation mimics flapping
        leftWing.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
        rightWing.localRotation = Quaternion.Euler(0.0f, 0.0f, -angle);
    }
}
