using UnityEngine;
using UnityEngine.UI;

public class EnvironmentBuilder : MonoBehaviour
{
    [Header("Generation Settings")]
    public bool generateOnStart = true;

    [Header("Material Colors")]
    public Color farmlandGreen = new Color(0.2f, 0.6f, 0.2f);
    public Color farmlandYellow = new Color(0.85f, 0.8f, 0.3f);
    public Color farmlandBrown = new Color(0.5f, 0.35f, 0.2f);
    public Color oceanBlue = new Color(0.0f, 0.3f, 0.7f);
    public Color yachtWhite = Color.white;
    public Color yachtWood = new Color(0.6f, 0.4f, 0.2f);
    public Color pegasusColor = Color.white;
    public Color PegasusManeColor = new Color(0.6f, 0.8f, 1.0f); // Light blue mane/tail
    
    private GameObject playerRig;

    void Start()
    {
        if (generateOnStart)
        {
            BuildAll();
        }
    }

    [ContextMenu("Build Scene")]
    public void BuildAll()
    {
        Debug.Log("Generating Stylized VR Pegasus Flying Environment...");

        // 1. Setup Lighting
        SetupLighting();

        // 2. Build Farmland (Left)
        BuildFarmland();

        // 3. Build Ocean & Yacht (Right)
        BuildOceanAndYacht();

        // 4. Build Pegasus & Player VR Rig
        BuildPegasusPlayer();

        // 5. Build Exit UI
        BuildExitUI();

        Debug.Log("Environment Generation Complete! Press Play to fly!");
    }

    private void SetupLighting()
    {
        // Add directional light if missing
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool hasDirLight = false;
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional) hasDirLight = true;
        }

        if (!hasDirLight)
        {
            GameObject lightGo = new GameObject("Sunlight");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.85f); // warm sunlight
        }
    }

    private void BuildFarmland()
    {
        GameObject farmlandContainer = new GameObject("Farmland_Grid");
        
        // Build a beautiful patchwork grid of fields
        int gridX = 8;
        int gridZ = 12;
        float fieldSize = 40.0f;

        Material greenMat = CreateMaterial(farmlandGreen, "FarmlandGreen");
        Material yellowMat = CreateMaterial(farmlandYellow, "FarmlandYellow");
        Material brownMat = CreateMaterial(farmlandBrown, "FarmlandBrown");

        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                GameObject field = GameObject.CreatePrimitive(PrimitiveType.Plane);
                field.name = $"Field_{x}_{z}";
                field.transform.parent = farmlandContainer.transform;
                
                // Position fields left of the ocean (X <= 0)
                field.transform.position = new Vector3((x - gridX) * fieldSize, 0.0f, z * fieldSize);
                field.transform.localScale = new Vector3(fieldSize / 10.0f, 1.0f, fieldSize / 10.0f);

                // Assign random farmland color
                int rand = (x + z) % 3;
                Renderer rend = field.GetComponent<Renderer>();
                if (rand == 0) rend.sharedMaterial = greenMat;
                else if (rand == 1) rend.sharedMaterial = yellowMat;
                else rend.sharedMaterial = brownMat;

                // Scatter farm decorations (barns, trees, windmills)
                if ((x + z) % 5 == 0)
                {
                    BuildStylizedBarn(field.transform.position, farmlandContainer.transform);
                }
                else if ((x + z) % 3 == 0)
                {
                    BuildStylizedTrees(field.transform.position, farmlandContainer.transform);
                }
                else if (x == 1 && z % 4 == 0)
                {
                    BuildWindmill(field.transform.position, farmlandContainer.transform);
                }
            }
        }
    }

    private void BuildStylizedBarn(Vector3 fieldPos, Transform parent)
    {
        GameObject barn = new GameObject("StylizedBarn");
        barn.transform.parent = parent;
        barn.transform.position = fieldPos + new Vector3(Random.Range(-5f, 5f), 0.0f, Random.Range(-5f, 5f));

        // Red Barn Base
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.transform.parent = barn.transform;
        baseObj.transform.localPosition = new Vector3(0.0f, 3.0f, 0.0f);
        baseObj.transform.localScale = new Vector3(8.0f, 6.0f, 12.0f);
        baseObj.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.7f, 0.15f, 0.15f), "BarnRed");

        // White Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.parent = barn.transform;
        roof.transform.localPosition = new Vector3(0.0f, 6.5f, 0.0f);
        roof.transform.localScale = new Vector3(9.0f, 2.0f, 13.0f);
        roof.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        roof.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.white, "BarnRoof");
    }

    private void BuildStylizedTrees(Vector3 fieldPos, Transform parent)
    {
        int treeCount = Random.Range(3, 7);
        Material trunkMat = CreateMaterial(new Color(0.45f, 0.25f, 0.1f), "TrunkBrown");
        Material leavesMat = CreateMaterial(new Color(0.15f, 0.5f, 0.15f), "LeavesGreen");

        for (int i = 0; i < treeCount; i++)
        {
            GameObject tree = new GameObject("StylizedTree");
            tree.transform.parent = parent;
            tree.transform.position = fieldPos + new Vector3(Random.Range(-12f, 12f), 0.0f, Random.Range(-12f, 12f));

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0.0f, 2.0f, 0.0f);
            trunk.transform.localScale = new Vector3(0.6f, 2.0f, 0.6f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

            // Foliage
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.transform.parent = tree.transform;
            foliage.transform.localPosition = new Vector3(0.0f, 4.5f, 0.0f);
            foliage.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
            foliage.GetComponent<Renderer>().sharedMaterial = leavesMat;
        }
    }

    private void BuildWindmill(Vector3 fieldPos, Transform parent)
    {
        GameObject windmill = new GameObject("Windmill");
        windmill.transform.parent = parent;
        windmill.transform.position = fieldPos + new Vector3(0f, 0f, 0f);

        // Tower
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tower.transform.parent = windmill.transform;
        tower.transform.localPosition = new Vector3(0.0f, 7.5f, 0.0f);
        tower.transform.localScale = new Vector3(3.0f, 7.5f, 3.0f);
        tower.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.85f, 0.85f, 0.85f), "WindmillGray");

        // Blades hub
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hub.transform.parent = windmill.transform;
        hub.transform.localPosition = new Vector3(0.0f, 14.5f, 2.0f);
        hub.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        hub.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.2f, 0.2f, 0.2f), "WindmillDark");

        // Simple blades (cross shape)
        GameObject blades = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blades.transform.parent = hub.transform;
        blades.transform.localPosition = Vector3.zero;
        blades.transform.localScale = new Vector3(8.0f, 0.6f, 0.1f);
        blades.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.white, "BladesWhite");

        GameObject bladesVertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bladesVertical.transform.parent = hub.transform;
        bladesVertical.transform.localPosition = Vector3.zero;
        bladesVertical.transform.localScale = new Vector3(0.6f, 8.0f, 0.1f);
        bladesVertical.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.white, "BladesWhite");

        // Add a rotating effect script
        hub.AddComponent<SimpleRotate>().rotateSpeed = 45.0f;
    }

    private void BuildOceanAndYacht()
    {
        GameObject oceanContainer = new GameObject("Ocean_Sector");

        // Massive Deep Blue Ocean Plane (On the Right)
        GameObject oceanPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        oceanPlane.name = "OceanSurface";
        oceanPlane.transform.parent = oceanContainer.transform;
        oceanPlane.transform.position = new Vector3(250.0f, -0.1f, 240.0f);
        oceanPlane.transform.localScale = new Vector3(50.0f, 1.0f, 50.0f); // 500x500 meters
        oceanPlane.GetComponent<Renderer>().sharedMaterial = CreateMaterial(oceanBlue, "OceanBlue");

        // Yacht (seen clearly from sky)
        BuildStylizedYacht(new Vector3(150.0f, 0.0f, 180.0f), oceanContainer.transform);
    }

    private void BuildStylizedYacht(Vector3 position, Transform parent)
    {
        GameObject yacht = new GameObject("StylizedYacht");
        yacht.transform.parent = parent;
        yacht.transform.position = position;
        yacht.transform.rotation = Quaternion.Euler(0.0f, -45.0f, 0.0f); // Face slightly diagonal

        Material whiteMat = CreateMaterial(yachtWhite, "YachtWhite");
        Material woodMat = CreateMaterial(yachtWood, "YachtWood");
        Material darkMat = CreateMaterial(new Color(0.1f, 0.1f, 0.1f), "YachtDark");

        // Outer Hull (White Capsule)
        GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.transform.parent = yacht.transform;
        hull.transform.localPosition = new Vector3(0.0f, 1.0f, 0.0f);
        hull.transform.localScale = new Vector3(7.0f, 2.5f, 22.0f);
        hull.GetComponent<Renderer>().sharedMaterial = whiteMat;

        // Pointy bow (Cone/Triangle simulation using a rotated box)
        GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.transform.parent = yacht.transform;
        bow.transform.localPosition = new Vector3(0.0f, 1.0f, 10.5f);
        bow.transform.localScale = new Vector3(5.0f, 2.5f, 5.0f);
        bow.transform.localRotation = Quaternion.Euler(0.0f, 45.0f, 0.0f);
        bow.GetComponent<Renderer>().sharedMaterial = whiteMat;

        // Wood Deck
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.transform.parent = yacht.transform;
        deck.transform.localPosition = new Vector3(0.0f, 2.26f, 0.0f);
        deck.transform.localScale = new Vector3(6.5f, 0.1f, 20.0f);
        deck.GetComponent<Renderer>().sharedMaterial = woodMat;

        // Main Cabin
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.parent = yacht.transform;
        cabin.transform.localPosition = new Vector3(0.0f, 3.5f, -2.0f);
        cabin.transform.localScale = new Vector3(5.0f, 2.5f, 10.0f);
        cabin.GetComponent<Renderer>().sharedMaterial = whiteMat;

        // Cabin Windows
        GameObject wind1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wind1.transform.parent = cabin.transform;
        wind1.transform.localPosition = new Vector3(0.0f, 0.3f, 0.51f);
        wind1.transform.localScale = new Vector3(0.9f, 0.4f, 0.1f);
        wind1.GetComponent<Renderer>().sharedMaterial = darkMat;

        // Mast
        GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.transform.parent = yacht.transform;
        mast.transform.localPosition = new Vector3(0.0f, 10.0f, 4.0f);
        mast.transform.localScale = new Vector3(0.4f, 8.0f, 0.4f);
        mast.GetComponent<Renderer>().sharedMaterial = woodMat;

        // Triangular Sail (using a rotated extremely thin box)
        GameObject sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sail.transform.parent = mast.transform;
        sail.transform.localPosition = new Vector3(0.0f, 2.0f, -4.0f);
        sail.transform.localRotation = Quaternion.Euler(20.0f, 0.0f, 0.0f);
        sail.transform.localScale = new Vector3(0.05f, 9.0f, 8.0f);
        sail.GetComponent<Renderer>().sharedMaterial = whiteMat;
    }

    private void BuildPegasusPlayer()
    {
        // 1. Create main player rig
        playerRig = new GameObject("PegasusRiderRig");
        // Start above farmland facing forward
        playerRig.transform.position = new Vector3(-80.0f, 25.0f, 40.0f);
        VRMotionController motion = playerRig.AddComponent<VRMotionController>();

        // 2. Create the Pegasus Model (child of Player Rig, placed slightly forward/down)
        GameObject pegasus = new GameObject("PegasusModel");
        pegasus.transform.parent = playerRig.transform;
        pegasus.transform.localPosition = new Vector3(0.0f, -1.8f, 1.2f);
        pegasus.transform.localRotation = Quaternion.identity;

        Material pegMat = CreateMaterial(pegasusColor, "PegasusWhite");
        Material maneMat = CreateMaterial(PegasusManeColor, "PegasusMane");
        Material eyeMat = CreateMaterial(Color.black, "PegasusEye");

        // Torso Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Torso";
        body.transform.parent = pegasus.transform;
        body.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(1.3f, 2.2f, 1.3f);
        body.GetComponent<Renderer>().sharedMaterial = pegMat;

        // Neck
        GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        neck.name = "Neck";
        neck.transform.parent = pegasus.transform;
        neck.transform.localPosition = new Vector3(0.0f, 1.4f, 1.4f);
        neck.transform.localRotation = Quaternion.Euler(-35.0f, 0.0f, 0.0f);
        neck.transform.localScale = new Vector3(0.8f, 2.0f, 0.8f);
        neck.GetComponent<Renderer>().sharedMaterial = pegMat;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.parent = neck.transform;
        head.transform.localPosition = new Vector3(0.0f, 1.2f, 0.4f);
        head.transform.localRotation = Quaternion.Euler(35.0f, 0.0f, 0.0f);
        head.transform.localScale = new Vector3(0.9f, 0.9f, 1.6f);
        head.GetComponent<Renderer>().sharedMaterial = pegMat;

        // Left Eye
        GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.transform.parent = head.transform;
        eyeL.transform.localPosition = new Vector3(-0.46f, 0.2f, 0.3f);
        eyeL.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        eyeL.GetComponent<Renderer>().sharedMaterial = eyeMat;

        // Right Eye
        GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.transform.parent = head.transform;
        eyeR.transform.localPosition = new Vector3(0.46f, 0.2f, 0.3f);
        eyeR.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        eyeR.GetComponent<Renderer>().sharedMaterial = eyeMat;

        // Ears
        GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        earL.transform.parent = head.transform;
        earL.transform.localPosition = new Vector3(-0.35f, 0.6f, -0.4f);
        earL.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        earL.GetComponent<Renderer>().sharedMaterial = pegMat;

        GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        earR.transform.parent = head.transform;
        earR.transform.localPosition = new Vector3(0.35f, 0.6f, -0.4f);
        earR.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        earR.GetComponent<Renderer>().sharedMaterial = pegMat;

        // Mane
        GameObject mane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mane.transform.parent = neck.transform;
        mane.transform.localPosition = new Vector3(0.0f, 0.0f, -0.45f);
        mane.transform.localScale = new Vector3(0.3f, 1.8f, 0.4f);
        mane.GetComponent<Renderer>().sharedMaterial = maneMat;

        // 4 Legs
        Vector3[] legOffsets = {
            new Vector3(-0.5f, -1.0f, 1.0f),  // Front Left
            new Vector3(0.5f, -1.0f, 1.0f),   // Front Right
            new Vector3(-0.5f, -1.0f, -1.0f), // Back Left
            new Vector3(0.5f, -1.0f, -1.0f)   // Back Right
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"Leg_{i}";
            leg.transform.parent = pegasus.transform;
            leg.transform.localPosition = legOffsets[i];
            leg.transform.localScale = new Vector3(0.3f, 1.0f, 0.3f);
            leg.GetComponent<Renderer>().sharedMaterial = pegMat;
        }

        // Tail
        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.transform.parent = pegasus.transform;
        tail.transform.localPosition = new Vector3(0.0f, 0.6f, -1.8f);
        tail.transform.localRotation = Quaternion.Euler(30.0f, 0.0f, 0.0f);
        tail.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
        tail.GetComponent<Renderer>().sharedMaterial = maneMat;

        // Wings setup
        GameObject lWingAnchor = new GameObject("LeftWingAnchor");
        lWingAnchor.transform.parent = pegasus.transform;
        lWingAnchor.transform.localPosition = new Vector3(-0.7f, 0.8f, 0.2f);

        GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWing.name = "LeftWingShape";
        leftWing.transform.parent = lWingAnchor.transform;
        leftWing.transform.localPosition = new Vector3(-2.0f, 0.0f, 0.0f);
        leftWing.transform.localScale = new Vector3(4.0f, 0.1f, 1.5f);
        leftWing.GetComponent<Renderer>().sharedMaterial = pegMat;

        GameObject rWingAnchor = new GameObject("RightWingAnchor");
        rWingAnchor.transform.parent = pegasus.transform;
        rWingAnchor.transform.localPosition = new Vector3(0.7f, 0.8f, 0.2f);

        GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWing.name = "RightWingShape";
        rightWing.transform.parent = rWingAnchor.transform;
        rightWing.transform.localPosition = new Vector3(2.0f, 0.0f, 0.0f);
        rightWing.transform.localScale = new Vector3(4.0f, 0.1f, 1.5f);
        rightWing.GetComponent<Renderer>().sharedMaterial = pegMat;

        // Add wing flaps animator
        PegasusAnimator anim = pegasus.AddComponent<PegasusAnimator>();
        anim.leftWing = lWingAnchor.transform;
        anim.rightWing = rWingAnchor.transform;

        // 3. Create VR Camera Rig
        GameObject cameraRigGo = new GameObject("VR_Camera_Rig");
        cameraRigGo.transform.parent = playerRig.transform;
        // Position camera right above/behind Pegasus neck, looking forward at head
        cameraRigGo.transform.localPosition = new Vector3(0.0f, 0.8f, -0.6f);
        cameraRigGo.transform.localRotation = Quaternion.identity;

        motion.cameraRig = cameraRigGo.transform;

        // Left Eye Camera
        GameObject leftCamGo = new GameObject("LeftEyeCamera");
        leftCamGo.transform.parent = cameraRigGo.transform;
        leftCamGo.transform.localPosition = new Vector3(-0.03f, 0.0f, 0.0f); // IPD offset
        leftCamGo.transform.localRotation = Quaternion.identity;
        Camera leftCam = leftCamGo.AddComponent<Camera>();
        leftCam.rect = new Rect(0.0f, 0.0f, 0.5f, 1.0f); // Left half of screen
        leftCam.fieldOfView = 80.0f;
        leftCam.nearClipPlane = 0.1f;
        leftCam.farClipPlane = 1000.0f;
        motion.leftEyeCamera = leftCamGo.transform;

        // Right Eye Camera
        GameObject rightCamGo = new GameObject("RightEyeCamera");
        rightCamGo.transform.parent = cameraRigGo.transform;
        rightCamGo.transform.localPosition = new Vector3(0.03f, 0.0f, 0.0f); // IPD offset
        rightCamGo.transform.localRotation = Quaternion.identity;
        Camera rightCam = rightCamGo.AddComponent<Camera>();
        rightCam.rect = new Rect(0.5f, 0.0f, 0.5f, 1.0f); // Right half of screen
        rightCam.fieldOfView = 80.0f;
        rightCam.nearClipPlane = 0.1f;
        rightCam.farClipPlane = 1000.0f;
        motion.rightEyeCamera = rightCamGo.transform;

        // Add skybox or background styling
        leftCam.backgroundColor = new Color(0.6f, 0.8f, 1.0f); // bright sky blue
        rightCam.backgroundColor = new Color(0.6f, 0.8f, 1.0f);
        leftCam.clearFlags = CameraClearFlags.Skybox;
        rightCam.clearFlags = CameraClearFlags.Skybox;
    }

    private void BuildExitUI()
    {
        // 1. Create Canvas
        GameObject canvasGo = new GameObject("ExitUICanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Topmost rendering
        
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<VRUIController>();

        // 2. Exit Button
        GameObject buttonGo = new GameObject("ExitButton");
        buttonGo.transform.parent = canvasGo.transform;
        
        RectTransform rt = buttonGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1.0f, 1.0f); // Top right anchor
        rt.anchorMax = new Vector2(1.0f, 1.0f);
        rt.pivot = new Vector2(1.0f, 1.0f);
        rt.anchoredPosition = new Vector3(-20.0f, -20.0f, 0.0f); // Offset from corner
        rt.sizeDelta = new Vector2(80.0f, 80.0f);

        Image btnImg = buttonGo.AddComponent<Image>();
        // Sleek semi-transparent dark red circular button
        btnImg.color = new Color(0.8f, 0.1f, 0.1f, 0.7f); 
        
        Button exitBtn = buttonGo.AddComponent<Button>();

        // Button label: "X"
        GameObject textGo = new GameObject("Text");
        textGo.transform.parent = buttonGo.transform;
        
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        Text text = textGo.AddComponent<Text>();
        text.text = "X";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private Material CreateMaterial(Color color, string name)
    {
        // Try Universal Render Pipeline shader first
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        
        // Ensure lighting works if standard
        if (shader.name == "Standard")
        {
            mat.SetFloat("_Glossiness", 0.1f);
        }
        return mat;
    }
}

// Inline script for rotating windmill blades
public class SimpleRotate : MonoBehaviour
{
    public float rotateSpeed = 45.0f;

    void Update()
    {
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime, Space.Self);
    }
}
