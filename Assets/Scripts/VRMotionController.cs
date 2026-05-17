using UnityEngine;
using UnityEngine.InputSystem;

public class VRMotionController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float forwardSpeed = 12.0f;
    public float upwardSpeed = 2.5f;
    public float steerSensitivity = 1.5f;
    public float decelerationRate = 1.5f; // How fast the Pegasus stops when walking stops
    public float accelerationRate = 2.0f; // How fast the Pegasus gains speed when walking starts

    [Header("Height Limits")]
    public float minFlightHeight = 10.0f;
    public float maxFlightHeight = 75.0f;

    [Header("Step Detection Settings")]
    [Tooltip("Sensitivity of walking detection. Lower is more sensitive.")]
    public float stepThreshold = 0.12f; // Change in G-force magnitude to trigger movement
    public float stepTimeout = 1.3f;    // Time in seconds to keep flying after a step

    [Header("Camera Rig References")]
    public Transform cameraRig;
    public Transform leftEyeCamera;
    public Transform rightEyeCamera;

    private float accAverage = 1.0f;
    private float stepTimer = 0.0f;
    private float currentSpeed = 0.0f;
    private float currentVerticalSpeed = 0.0f;
    private bool isFlying = false;

    // Fallback gyro tracking fields
    private bool gyroEnabled = false;
    private Quaternion gyroBaseRotation = Quaternion.identity;

    void Start()
    {
        // Enable Gyroscope for head tracking fallback
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroEnabled = true;
            gyroBaseRotation = Input.gyro.attitude;
            Debug.Log("Gyroscope initialized successfully.");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device. Fallback to mouse/keyboard inputs in Editor.");
        }

        // Initialize average acceleration magnitude
        accAverage = Input.acceleration.magnitude;
        if (accAverage < 0.1f) accAverage = 1.0f; // Avoid division by zero/uninitialized states
    }

    void Update()
    {
        HandleHeadTracking();
        HandleStepDetection();
        HandleFlightMovement();
    }

    private void HandleHeadTracking()
    {
        // If Built-in XR is active, it will automatically handle camera tracking.
        // We only apply manual gyro tracking if XR is not active or in Editor.
        bool isXRActive = UnityEngine.XR.XRSettings.enabled;
        
        if (!isXRActive && gyroEnabled)
        {
            // Get raw gyroscope attitude
            Quaternion gyroAttitude = Input.gyro.attitude;
            
            // Map right-handed sensor coordinates to left-handed Unity coordinates.
            // Under landscape-left orientation, this converts sensor inputs cleanly.
            Quaternion rawRotation = new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
            
            // Rotate 90 degrees around X and Y to align phone landscape orientation with camera forward
            Quaternion cameraOrientation = Quaternion.Euler(90f, 90f, 0f) * rawRotation;
            
            if (cameraRig != null)
            {
                cameraRig.localRotation = cameraOrientation;
            }
        }
        else if (Application.isEditor && cameraRig != null)
        {
            // Simple Editor mouse looking fallback: Right click and drag to look around
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * 3.0f;
                float mouseY = Input.GetAxis("Mouse Y") * 3.0f;
                cameraRig.Rotate(Vector3.up, mouseX, Space.World);
                cameraRig.Rotate(Vector3.right, -mouseY, Space.Self);
            }
            
            // Editor key tilts (Q/E for head roll simulation)
            float rollInput = 0f;
            if (Input.GetKey(KeyCode.Q)) rollInput = 15f;
            if (Input.GetKey(KeyCode.E)) rollInput = -15f;
            if (rollInput != 0f)
            {
                cameraRig.localRotation = Quaternion.Euler(cameraRig.localRotation.eulerAngles.x, cameraRig.localRotation.eulerAngles.y, rollInput);
            }
        }
    }

    private void HandleStepDetection()
    {
        // In Editor, space bar or W key simulates jogging/walking in place
        if (Application.isEditor)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space))
            {
                isFlying = true;
                stepTimer = stepTimeout;
                return;
            }
        }

        // Real device acceleration tracking
        float currentAccMagnitude = Input.acceleration.magnitude;
        
        // Track a moving average of G-forces to isolate dynamic bounce from constant gravity
        accAverage = Mathf.Lerp(accAverage, currentAccMagnitude, Time.deltaTime * 3.0f);
        
        // Dynamic acceleration represents the vertical bounce of jogging
        float dynamicAcc = Mathf.Abs(currentAccMagnitude - accAverage);

        // If bounce exceeds threshold (indicates phone moving up/down by >= 0.5 inches)
        if (dynamicAcc > stepThreshold)
        {
            isFlying = true;
            stepTimer = stepTimeout;
        }

        // Countdown flight timer
        if (stepTimer > 0.0f)
        {
            stepTimer -= Time.deltaTime;
        }
        else
        {
            isFlying = false;
        }
    }

    private void HandleFlightMovement()
    {
        // 1. Steering by head tilt (roll axis)
        float rollAngle = 0.0f;
        
        if (cameraRig != null)
        {
            // Extract roll (Z-axis rotation) from the camera rig
            rollAngle = cameraRig.localEulerAngles.z;
        }
        else if (leftEyeCamera != null)
        {
            rollAngle = leftEyeCamera.localEulerAngles.z;
        }

        // Convert 0..360 range to -180..180 range
        if (rollAngle > 180.0f)
        {
            rollAngle -= 360.0f;
        }

        // Steer the Pegasus (rotate this parent GameObject yaw/Y-axis) based on tilt
        // Tilting left (negative roll) turns left (negative yaw). Tilting right turns right.
        float steerAmount = 0.0f;
        
        // Apply deadzone to avoid tiny head tilts triggering unwanted turns
        if (Mathf.Abs(rollAngle) > 2.5f)
        {
            // Steer proportional to the tilt angle (roll)
            steerAmount = -rollAngle * steerSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up, steerAmount, Space.World);
        }

        // 2. Flight Acceleration & Movement
        float targetForwardSpeed = isFlying ? forwardSpeed : 0.0f;
        float targetVerticalSpeed = isFlying ? upwardSpeed : -decelerationRate; // Descend slowly if not walking

        currentSpeed = Mathf.Lerp(currentSpeed, targetForwardSpeed, Time.deltaTime * (isFlying ? accelerationRate : decelerationRate));
        currentVerticalSpeed = Mathf.Lerp(currentVerticalSpeed, targetVerticalSpeed, Time.deltaTime * (isFlying ? accelerationRate : decelerationRate));

        // Calculate translation vector
        Vector3 moveDirection = transform.forward * currentSpeed * Time.deltaTime;
        moveDirection.y = currentVerticalSpeed * Time.deltaTime;

        // Apply movement
        transform.Translate(moveDirection, Space.World);

        // 3. Height clamping to stay above ground and ocean
        Vector3 clampedPosition = transform.position;
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minFlightHeight, maxFlightHeight);
        transform.position = clampedPosition;
    }

    // Public getter for UI or Pegasus animation scripts to check flight speed/state
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public bool IsCurrentlyFlying()
    {
        return isFlying;
    }
}
