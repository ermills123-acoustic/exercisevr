using UnityEngine;

public class VRMotionController : MonoBehaviour
{
    public enum MovementState
    {
        Walking,
        Swimming,
        Flying,
        Gliding
    }

    [Header("State Machine")]
    public MovementState currentState = MovementState.Walking;

    [Header("Flight Settings")]
    public float flyForwardSpeed = 12.0f;
    public float flyUpwardSpeed = 2.5f;
    public float glideDecelRate = 1.5f;
    
    [Header("Grounded Walking Settings")]
    public float walkSpeed = 2.0f;
    
    [Header("Swimming Settings")]
    public float swimSpeed = 1.5f;
    public float waterBobbingSpeed = 2.0f;
    public float waterBobbingAmp = 0.12f;

    [Header("Steering Settings")]
    public float steerSensitivity = 1.5f;
    public float rollDeadzone = 2.5f;

    [Header("Height Limits")]
    public float maxFlightHeight = 85.0f;
    public float seaLevel = 0.0f;

    [Header("Step Detection Settings")]
    [Tooltip("Sensitivity of dynamic up-down movement bounce.")]
    public float stepThreshold = 0.12f; 
    public float stepTimeout = 1.3f;    

    [Header("Camera Rig References")]
    public Transform cameraRig;
    public Transform leftEyeCamera;
    public Transform rightEyeCamera;

    [Header("Leg & Wing Model Animate Anchors")]
    public Transform leftLegAnchor;
    public Transform rightLegAnchor;
    public Transform leftWingAnchor;
    public Transform rightWingAnchor;

    private float accAverage = 1.0f;
    private float stepTimer = 0.0f;
    private float currentSpeed = 0.0f;
    private float currentVerticalSpeed = 0.0f;
    private bool isJogging = false;
    
    // Rigidbody and physics boundary state
    private Rigidbody rb;
    private float groundHeight = 0.0f;
    private bool isGrounded = true;

    // Fallback gyro fields
    private bool gyroEnabled = false;

    void Start()
    {
        // 1. Initialize Rigidbody for physical collision boundaries
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // RigidBody setup for VR locomotion
        rb.useGravity = false; // We process vertical gravity and buoyancy manually to prevent VR camera jitter!
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Don't fall over when hitting houses!
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 2. Add CapsuleCollider if missing
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap == null)
        {
            cap = gameObject.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0.0f, 0.0f, 0.0f);
            cap.radius = 1.2f;
            cap.height = 3.0f;
            cap.direction = 2; // Z-Axis capsule
        }

        // 3. Initialize Gyro fallback
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroEnabled = true;
        }

        accAverage = Input.acceleration.magnitude;
        if (accAverage < 0.1f) accAverage = 1.0f;
    }

    void Update()
    {
        HandleHeadTracking();
        HandleStepDetection();
        UpdateTerrainElevation();
        UpdateMovementState();
        HandleSteering();
        ApplyMovement();
        AnimateLegsAndWings();
    }

    private void HandleHeadTracking()
    {
        bool isXRActive = UnityEngine.XR.XRSettings.enabled;
        
        if (!isXRActive && gyroEnabled)
        {
            Quaternion gyroAttitude = Input.gyro.attitude;
            Quaternion rawRotation = new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
            Quaternion cameraOrientation = Quaternion.Euler(90f, 90f, 0f) * rawRotation;
            
            if (cameraRig != null)
            {
                cameraRig.localRotation = cameraOrientation;
            }
        }
        else if (Application.isEditor && cameraRig != null)
        {
            // Right-click drag looking
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * 3.0f;
                float mouseY = Input.GetAxis("Mouse Y") * 3.0f;
                cameraRig.Rotate(Vector3.up, mouseX, Space.World);
                cameraRig.Rotate(Vector3.right, -mouseY, Space.Self);
            }
            
            // Q/E Roll simulation
            float rollInput = 0f;
            if (Input.GetKey(KeyCode.Q)) rollInput = 15f;
            if (Input.GetKey(KeyCode.E)) rollInput = -15f;
            if (rollInput != 0f)
            {
                cameraRig.localRotation = Quaternion.Euler(cameraRig.localRotation.eulerAngles.x, cameraRig.localRotation.eulerAngles.y, rollInput);
            }
            else
            {
                cameraRig.localRotation = Quaternion.Euler(cameraRig.localRotation.eulerAngles.x, cameraRig.localRotation.eulerAngles.y, 0f);
            }
        }
    }

    private void HandleStepDetection()
    {
        if (Application.isEditor)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space))
            {
                isJogging = true;
                stepTimer = stepTimeout;
                return;
            }
        }

        float currentAccMagnitude = Input.acceleration.magnitude;
        accAverage = Mathf.Lerp(accAverage, currentAccMagnitude, Time.deltaTime * 3.0f);
        float dynamicAcc = Mathf.Abs(currentAccMagnitude - accAverage);

        if (dynamicAcc > stepThreshold)
        {
            isJogging = true;
            stepTimer = stepTimeout;
        }

        if (stepTimer > 0.0f)
        {
            stepTimer -= Time.deltaTime;
        }
        else
        {
            isJogging = false;
        }
    }

    private void UpdateTerrainElevation()
    {
        // Sample height of infinite terrain dynamically using the same Perlin noise formula
        float px = transform.position.x;
        float pz = transform.position.z;
        
        // Farmland hills terrain formula
        if (px < 0f)
        {
            groundHeight = Mathf.PerlinNoise(px * 0.006f, pz * 0.006f) * 12.0f;
        }
        else
        {
            groundHeight = -10.0f; // ocean channel is deep
        }
    }

    private void UpdateMovementState()
    {
        float currentY = transform.position.y;

        if (isJogging)
        {
            currentState = MovementState.Flying;
        }
        else
        {
            if (currentY <= seaLevel + 0.1f && transform.position.x >= -5.0f)
            {
                currentState = MovementState.Swimming;
            }
            else if (currentY <= groundHeight + 0.1f && transform.position.x < -5.0f)
            {
                currentState = MovementState.Walking;
            }
            else
            {
                currentState = MovementState.Gliding;
            }
        }
    }

    private void HandleSteering()
    {
        float rollAngle = 0.0f;
        if (cameraRig != null)
        {
            rollAngle = cameraRig.localEulerAngles.z;
        }
        else if (leftEyeCamera != null)
        {
            rollAngle = leftEyeCamera.localEulerAngles.z;
        }

        if (rollAngle > 180.0f)
        {
            rollAngle -= 360.0f;
        }

        if (Mathf.Abs(rollAngle) > rollDeadzone)
        {
            float steerAmount = -rollAngle * steerSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up, steerAmount, Space.World);
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = 0.0f;
        float targetVertSpeed = 0.0f;

        switch (currentState)
        {
            case MovementState.Walking:
                targetSpeed = walkSpeed;
                // Stick to rolling hills ground height
                float targetWalkY = groundHeight;
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, targetWalkY, Time.deltaTime * 5f), transform.position.z);
                targetVertSpeed = 0f;
                break;

            case MovementState.Swimming:
                targetSpeed = swimSpeed;
                // Bob gently on the ocean wave level
                float targetSwimY = seaLevel + Mathf.Sin(Time.time * waterBobbingSpeed) * waterBobbingAmp;
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, targetSwimY, Time.deltaTime * 5f), transform.position.z);
                targetVertSpeed = 0f;
                break;

            case MovementState.Flying:
                targetSpeed = flyForwardSpeed;
                targetVertSpeed = flyUpwardSpeed;
                break;

            case MovementState.Gliding:
                targetSpeed = flyForwardSpeed * 0.7f;
                targetVertSpeed = -glideDecelRate; // Descend slowly
                break;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 3.0f);
        currentVerticalSpeed = Mathf.Lerp(currentVerticalSpeed, targetVertSpeed, Time.deltaTime * 2.0f);

        // Calculate direct translation
        Vector3 forwardMove = transform.forward * currentSpeed;
        forwardMove.y = currentVerticalSpeed;

        // Apply velocity to rigidbody so physics collisions work beautifully with trees/houses!
        rb.velocity = forwardMove;

        // Secure bounds checking to prevent flying into space
        Vector3 pos = transform.position;
        if (pos.y > maxFlightHeight)
        {
            pos.y = maxFlightHeight;
            transform.position = pos;
            rb.velocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
        }
        // Clamping lower height boundaries depending on area
        if (pos.x < -5.0f) // Farmland ground clamp
        {
            if (pos.y < groundHeight)
            {
                pos.y = groundHeight;
                transform.position = pos;
            }
        }
        else // Ocean ground clamp
        {
            if (pos.y < seaLevel - 2.0f)
            {
                pos.y = seaLevel - 2.0f;
                transform.position = pos;
            }
        }
    }

    private void AnimateLegsAndWings()
    {
        // Dynamic wing flapping based on movement speed & state
        if (leftWingAnchor != null && rightWingAnchor != null)
        {
            float flapSpeed = 4.0f;
            float maxFlapAngle = 20.0f;

            if (currentState == MovementState.Flying)
            {
                flapSpeed = 14.0f;
                maxFlapAngle = 35.0f;
            }
            else if (currentState == MovementState.Gliding)
            {
                flapSpeed = 3.0f;
                maxFlapAngle = 10.0f;
            }
            else // Walking or Swimming
            {
                flapSpeed = 1.0f;
                maxFlapAngle = 4.0f;
            }

            float angle = Mathf.Sin(Time.time * flapSpeed) * maxFlapAngle;
            leftWingAnchor.localRotation = Quaternion.Euler(0f, 0f, angle);
            rightWingAnchor.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // Galloping leg animations when moving!
        if (leftLegAnchor != null && rightLegAnchor != null)
        {
            float gallopSpeed = 0.0f;
            float maxLegAngle = 25.0f;

            if (currentState == MovementState.Walking)
            {
                gallopSpeed = 8.0f;
            }
            else if (currentState == MovementState.Flying)
            {
                gallopSpeed = 12.0f;
                maxLegAngle = 35.0f;
            }
            else if (currentState == MovementState.Swimming)
            {
                gallopSpeed = 4.0f;
                maxLegAngle = 15.0f;
            }

            if (gallopSpeed > 0f)
            {
                float leftAngle = Mathf.Sin(Time.time * gallopSpeed) * maxLegAngle;
                float rightAngle = Mathf.Sin(Time.time * gallopSpeed + Mathf.PI) * maxLegAngle;
                leftLegAnchor.localRotation = Quaternion.Euler(leftAngle, 0f, 0f);
                rightLegAnchor.localRotation = Quaternion.Euler(rightAngle, 0f, 0f);
            }
            else
            {
                // Relaxed idle dangle position
                leftLegAnchor.localRotation = Quaternion.Lerp(leftLegAnchor.localRotation, Quaternion.Euler(10f, 0f, 0f), Time.deltaTime * 3.0f);
                rightLegAnchor.localRotation = Quaternion.Lerp(rightLegAnchor.localRotation, Quaternion.Euler(10f, 0f, 0f), Time.deltaTime * 3.0f);
            }
        }
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public bool IsCurrentlyFlying()
    {
        return currentState == MovementState.Flying;
    }
}
