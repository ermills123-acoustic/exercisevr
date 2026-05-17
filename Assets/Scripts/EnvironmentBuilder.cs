using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class EnvironmentBuilder : MonoBehaviour
{
    [Header("Chunk Settings")]
    public float chunkCellSize = 160.0f; // Width and length of each terrain chunk
    public bool generateOnStart = true;

    [Header("Realistic Materials")]
    private Material farmlandMaterial;
    private Material oceanMaterial;
    private Material roadMaterial;
    private Material wallMaterial;
    private Material roofMaterial;
    private Material barkMaterial;
    private Material leavesMaterial;

    [Header("Pegasus Layered Materials")]
    private Material pegasusHeadMat;
    private Material pegasusWingMat;
    private Material pegasusLegMat;
    private Material pegasusBodyMat;

    private GameObject playerRig;
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerCell = new Vector2Int(-999, -999);

    void Start()
    {
        InitializeMaterials();

        if (generateOnStart)
        {
            BuildAll();
        }
    }

    [ContextMenu("Build Scene")]
    public void BuildAll()
    {
        Debug.Log("Generating Infinite Photorealistic VR Pegasus Environment...");

        // 1. Setup lighting
        SetupLighting();

        // 2. Build the high-fidelity Pegasus and VR player rig
        BuildPegasusPlayer();

        // 3. Force first-frame chunk streaming around the player
        StreamChunks(true);

        // 4. Build Exit UI
        BuildExitUI();
    }

    void Update()
    {
        if (playerRig != null)
        {
            StreamChunks(false);
        }
    }

    private void InitializeMaterials()
    {
        // Load textures from Assets/Textures/ (copied from AI generated assets)
        Texture2D farmTex = LoadPNG("farmland_texture.png");
        Texture2D oceanTex = LoadPNG("ocean_texture.png");
        Texture2D roadTex = LoadPNG("road_texture.png");
        Texture2D wallTex = LoadPNG("house_wall.png");
        Texture2D roofTex = LoadPNG("house_roof.png");
        Texture2D barkTex = LoadPNG("bark_texture.png");

        // Fallbacks using procedural texture generators
        Texture2D leavesTex = ProceduralTextureHelper.GenerateLeavesTexture();

        farmlandMaterial = CreateStandardMaterial(farmTex, new Color(0.2f, 0.5f, 0.2f), "FarmlandMat");
        oceanMaterial = CreateStandardMaterial(oceanTex, new Color(0.0f, 0.2f, 0.6f), "OceanMat");
        // Shimmering glossy water setup
        oceanMaterial.SetFloat("_Glossiness", 0.85f);
        
        roadMaterial = CreateStandardMaterial(roadTex, new Color(0.5f, 0.5f, 0.5f), "RoadMat");
        wallMaterial = CreateStandardMaterial(wallTex, new Color(0.8f, 0.8f, 0.8f), "WallMat");
        roofMaterial = CreateStandardMaterial(roofTex, new Color(0.8f, 0.3f, 0.3f), "RoofMat");
        barkMaterial = CreateStandardMaterial(barkTex, new Color(0.4f, 0.3f, 0.2f), "BarkMat");
        leavesMaterial = CreateStandardMaterial(leavesTex, new Color(0.1f, 0.5f, 0.1f), "LeavesMat");

        // Generate photorealistic layered Pegasus body part textures
        Texture2D pegHeadTex = ProceduralTextureHelper.GeneratePegasusHeadTexture();
        Texture2D pegWingTex = ProceduralTextureHelper.GeneratePegasusWingTexture();
        Texture2D pegLegTex = ProceduralTextureHelper.GeneratePegasusLegTexture();

        pegasusHeadMat = CreateTransparentMaterial(pegHeadTex, "PegasusHeadMat");
        pegasusWingMat = CreateTransparentMaterial(pegWingTex, "PegasusWingMat");
        pegasusLegMat = CreateTransparentMaterial(pegLegTex, "PegasusLegMat");
    }

    private void SetupLighting()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool hasDirLight = false;
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional) hasDirLight = true;
        }

        if (!hasDirLight)
        {
            GameObject lightGo = new GameObject("WarmSunlight");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.95f, 0.9f);
        }
    }

    private void BuildPegasusPlayer()
    {
        // 1. Create Player Rig
        playerRig = new GameObject("PegasusRiderRig");
        playerRig.transform.position = new Vector3(-80.0f, 15.0f, 40.0f);
        VRMotionController motion = playerRig.AddComponent<VRMotionController>();

        // 2. Create VR Camera Rig
        GameObject cameraRigGo = new GameObject("VR_Camera_Rig");
        cameraRigGo.transform.parent = playerRig.transform;
        cameraRigGo.transform.localPosition = new Vector3(0.0f, 0.8f, -0.6f);
        cameraRigGo.transform.localRotation = Quaternion.identity;
        motion.cameraRig = cameraRigGo.transform;

        // Left Eye Camera
        GameObject leftCamGo = new GameObject("LeftEyeCamera");
        leftCamGo.transform.parent = cameraRigGo.transform;
        leftCamGo.transform.localPosition = new Vector3(-0.03f, 0.0f, 0.0f);
        Camera leftCam = leftCamGo.AddComponent<Camera>();
        leftCam.rect = new Rect(0.0f, 0.0f, 0.5f, 1.0f);
        leftCam.fieldOfView = 80.0f;
        leftCam.nearClipPlane = 0.1f;
        leftCam.farClipPlane = 1000.0f;
        motion.leftEyeCamera = leftCamGo.transform;

        // Right Eye Camera
        GameObject rightCamGo = new GameObject("RightEyeCamera");
        rightCamGo.transform.parent = cameraRigGo.transform;
        rightCamGo.transform.localPosition = new Vector3(0.03f, 0.0f, 0.0f);
        Camera rightCam = rightCamGo.AddComponent<Camera>();
        rightCam.rect = new Rect(0.5f, 0.0f, 0.5f, 1.0f);
        rightCam.fieldOfView = 80.0f;
        rightCam.nearClipPlane = 0.1f;
        rightCam.farClipPlane = 1000.0f;
        motion.rightEyeCamera = rightCamGo.transform;

        leftCam.clearFlags = CameraClearFlags.Skybox;
        rightCam.clearFlags = CameraClearFlags.Skybox;

        // 3. Build Layered Pegasus Model (Visual quads for photorealism)
        GameObject pegasus = new GameObject("PegasusModel");
        pegasus.transform.parent = playerRig.transform;
        pegasus.transform.localPosition = Vector3.zero;
        pegasus.transform.localRotation = Quaternion.identity;

        // Neck and Head Quad (child of Camera Rig so it stays perfectly oriented in front of rider view)
        GameObject neckHead = GameObject.CreatePrimitive(PrimitiveType.Quad);
        neckHead.name = "PegasusHeadNeck";
        Destroy(neckHead.GetComponent<Collider>()); // No self collision
        neckHead.transform.parent = cameraRigGo.transform;
        // Positioned forward and slightly down, facing camera
        neckHead.transform.localPosition = new Vector3(0.0f, -0.4f, 1.3f);
        neckHead.transform.localRotation = Quaternion.identity; 
        neckHead.transform.localScale = new Vector3(1.6f, 1.6f, 1.0f);
        neckHead.GetComponent<Renderer>().sharedMaterial = pegasusHeadMat;

        // Left Wing Anchor & Quad
        GameObject lWingAnchor = new GameObject("LeftWingAnchor");
        lWingAnchor.transform.parent = playerRig.transform;
        lWingAnchor.transform.localPosition = new Vector3(-0.8f, 0.2f, 0.0f);
        motion.leftWingAnchor = lWingAnchor.transform;

        GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leftWing.name = "LeftWingQuad";
        Destroy(leftWing.GetComponent<Collider>());
        leftWing.transform.parent = lWingAnchor.transform;
        leftWing.transform.localPosition = new Vector3(-1.6f, 0.0f, 0.4f);
        leftWing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lay flat
        leftWing.transform.localScale = new Vector3(3.2f, 3.2f, 1.0f);
        leftWing.GetComponent<Renderer>().sharedMaterial = pegasusWingMat;

        // Right Wing Anchor & Quad
        GameObject rWingAnchor = new GameObject("RightWingAnchor");
        rWingAnchor.transform.parent = playerRig.transform;
        rWingAnchor.transform.localPosition = new Vector3(0.8f, 0.2f, 0.0f);
        motion.rightWingAnchor = rWingAnchor.transform;

        GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        rightWing.name = "RightWingQuad";
        Destroy(rightWing.GetComponent<Collider>());
        rightWing.transform.parent = rWingAnchor.transform;
        rightWing.transform.localPosition = new Vector3(1.6f, 0.0f, 0.4f);
        rightWing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rightWing.transform.localScale = new Vector3(-3.2f, 3.2f, 1.0f); // X-Mirror
        rightWing.GetComponent<Renderer>().sharedMaterial = pegasusWingMat;

        // Left Front Leg Anchor & Quad
        GameObject lLegAnchor = new GameObject("LeftLegAnchor");
        lLegAnchor.transform.parent = playerRig.transform;
        lLegAnchor.transform.localPosition = new Vector3(-0.4f, -0.4f, 0.5f);
        motion.leftLegAnchor = lLegAnchor.transform;

        GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leftLeg.name = "LeftLegQuad";
        Destroy(leftLeg.GetComponent<Collider>());
        leftLeg.transform.parent = lLegAnchor.transform;
        leftLeg.transform.localPosition = new Vector3(0.0f, -0.6f, 0.0f);
        leftLeg.transform.localRotation = Quaternion.identity;
        leftLeg.transform.localScale = new Vector3(0.7f, 1.4f, 1.0f);
        leftLeg.GetComponent<Renderer>().sharedMaterial = pegasusLegMat;

        // Right Front Leg Anchor & Quad
        GameObject rLegAnchor = new GameObject("RightLegAnchor");
        rLegAnchor.transform.parent = playerRig.transform;
        rLegAnchor.transform.localPosition = new Vector3(0.4f, -0.4f, 0.5f);
        motion.rightLegAnchor = rLegAnchor.transform;

        GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        rightLeg.name = "RightLegQuad";
        Destroy(rightLeg.GetComponent<Collider>());
        rightLeg.transform.parent = rLegAnchor.transform;
        rightLeg.transform.localPosition = new Vector3(0.0f, -0.6f, 0.0f);
        rightLeg.transform.localRotation = Quaternion.identity;
        rightLeg.transform.localScale = new Vector3(-0.7f, 1.4f, 1.0f); // X-Mirror
        rightLeg.GetComponent<Renderer>().sharedMaterial = pegasusLegMat;
    }

    private void StreamChunks(bool forceUpdate)
    {
        Vector3 pPos = playerRig.transform.position;
        int pCellX = Mathf.FloorToInt(pPos.x / chunkCellSize);
        int pCellZ = Mathf.FloorToInt(pPos.z / chunkCellSize);
        Vector2Int currentCell = new Vector2Int(pCellX, pCellZ);

        if (currentCell == lastPlayerCell && !forceUpdate) return;
        lastPlayerCell = currentCell;

        // 1. Spawning active grid (3x3 chunks around player)
        List<Vector2Int> desiredCells = new List<Vector2Int>();
        for (int x = pCellX - 1; x <= pCellX + 1; x++)
        {
            for (int z = pCellZ - 1; z <= pCellZ + 1; z++)
            {
                desiredCells.Add(new Vector2Int(x, z));
            }
        }

        // Spawn missing chunks
        foreach (var cell in desiredCells)
        {
            if (!activeChunks.ContainsKey(cell))
            {
                GameObject chunkGo = SpawnChunk(cell.x, cell.y);
                activeChunks.Add(cell, chunkGo);
            }
        }

        // 2. Recycle out of range chunks
        List<Vector2Int> cellsToRemove = new List<Vector2Int>();
        foreach (var cell in activeChunks.Keys)
        {
            if (!desiredCells.Contains(cell))
            {
                cellsToRemove.Add(cell);
            }
        }

        foreach (var cell in cellsToRemove)
        {
            GameObject chunkGo = activeChunks[cell];
            Destroy(chunkGo);
            activeChunks.Remove(cell);
        }
    }

    private GameObject SpawnChunk(int cx, int cz)
    {
        GameObject chunkContainer = new GameObject($"Chunk_{cx}_{cz}");
        chunkContainer.transform.position = new Vector3(cx * chunkCellSize, 0f, cz * chunkCellSize);

        // Determine Chunk Type based on X coordinate
        // cx < -1: Farmland, -1 <= cx <= 0: Shoreline, cx > 0: Shimmering Ocean
        if (cx < -1)
        {
            BuildFarmlandChunk(chunkContainer, cx, cz);
        }
        else if (cx <= 0)
        {
            BuildCoastlineChunk(chunkContainer, cx, cz);
        }
        else
        {
            BuildOceanChunk(chunkContainer, cx, cz);
        }

        return chunkContainer;
    }

    private void BuildFarmlandChunk(GameObject parent, int cx, int cz)
    {
        // Generate physical rolling hills terrain
        GameObject terrain = CreateChunkTerrainMesh(cx, cz, farmlandMaterial);
        terrain.transform.parent = parent.transform;
        terrain.transform.localPosition = Vector3.zero;

        // Spawn a road running through the farm
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.name = "CountryRoad";
        Destroy(road.GetComponent<Collider>());
        road.transform.parent = parent.transform;
        road.transform.localPosition = new Vector3(chunkCellSize * 0.5f, 0.05f, chunkCellSize * 0.5f);
        road.transform.localScale = new Vector3(2.5f, 1.0f, chunkCellSize / 10f); // 25 meters wide asphalt path
        road.GetComponent<Renderer>().sharedMaterial = roadMaterial;

        // Scatter collidable farm features (barns, trees, windmills, houses)
        // Position calculations take Perlin noise height into account to stick them accurately to the surface!
        float step = chunkCellSize / 4f;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                // Avoid spawning directly on the road
                if (i == 2) continue;

                Vector3 localPos = new Vector3(i * step + step * 0.5f, 0f, j * step + step * 0.5f);
                Vector3 worldPos = parent.transform.position + localPos;
                worldPos.y = Mathf.PerlinNoise(worldPos.x * 0.006f, worldPos.z * 0.006f) * 12.0f;
                localPos.y = worldPos.y;

                int rand = Mathf.Abs((cx + cz + i + j) % 5);
                if (rand == 0)
                {
                    BuildRealHouse(localPos, parent.transform);
                }
                else if (rand == 1)
                {
                    BuildRealBarn(localPos, parent.transform);
                }
                else if (rand == 2)
                {
                    BuildRealWindmill(localPos, parent.transform);
                }
                else
                {
                    BuildRealTrees(localPos, parent.transform);
                }
            }
        }
    }

    private void BuildCoastlineChunk(GameObject parent, int cx, int cz)
    {
        // Slope sloping terrain
        GameObject terrain = CreateChunkTerrainMesh(cx, cz, farmlandMaterial);
        terrain.transform.parent = parent.transform;
        terrain.transform.localPosition = Vector3.zero;

        // Ocean water plane overlays
        GameObject oceanPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        oceanPlane.name = "ShoreWater";
        Destroy(oceanPlane.GetComponent<Collider>()); // Buoyancy processed via VRMotionController
        oceanPlane.transform.parent = parent.transform;
        oceanPlane.transform.localPosition = new Vector3(chunkCellSize * 0.5f, 0.0f, chunkCellSize * 0.5f);
        oceanPlane.transform.localScale = new Vector3(chunkCellSize / 10.0f, 1.0f, chunkCellSize / 10.0f);
        oceanPlane.GetComponent<Renderer>().sharedMaterial = oceanMaterial;

        // Add physical sandy hills trees
        BuildRealTrees(new Vector3(chunkCellSize * 0.2f, 1.0f, chunkCellSize * 0.5f), parent.transform);
    }

    private void BuildOceanChunk(GameObject parent, int cx, int cz)
    {
        // Water surface plane with gorgeous shimmering texture
        GameObject oceanPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        oceanPlane.name = "OceanSurface";
        Destroy(oceanPlane.GetComponent<Collider>());
        oceanPlane.transform.parent = parent.transform;
        oceanPlane.transform.localPosition = new Vector3(chunkCellSize * 0.5f, 0.0f, chunkCellSize * 0.5f);
        oceanPlane.transform.localScale = new Vector3(chunkCellSize / 10.0f, 1.0f, chunkCellSize / 10.0f);
        oceanPlane.GetComponent<Renderer>().sharedMaterial = oceanMaterial;

        // Occasional yachts spawned on ocean
        if (Mathf.Abs((cx + cz) % 3) == 0)
        {
            BuildRealYacht(new Vector3(chunkCellSize * 0.5f, 0.0f, chunkCellSize * 0.5f), parent.transform);
        }
    }

    private GameObject CreateChunkTerrainMesh(int cx, int cz, Material mat)
    {
        GameObject go = new GameObject("TerrainGrid");
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        int res = 8;
        int numVerts = (res + 1) * (res + 1);
        Vector3[] vertices = new Vector3[numVerts];
        Vector2[] uv = new Vector2[numVerts];
        int[] triangles = new int[res * res * 6];

        float worldOffsetX = cx * chunkCellSize;
        float worldOffsetZ = cz * chunkCellSize;

        for (int z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++)
            {
                int idx = z * (res + 1) + x;
                float px = worldOffsetX + (x / (float)res) * chunkCellSize;
                float pz = worldOffsetZ + (z / (float)res) * chunkCellSize;

                float py = 0.0f;
                if (px < -5.0f) // Farmland hills
                {
                    py = Mathf.PerlinNoise(px * 0.006f, pz * 0.006f) * 12.0f;
                }
                else if (px < 15.0f) // Shoreline smooth slope transition
                {
                    float t = Mathf.InverseLerp(-5.0f, 15.0f, px);
                    float landH = Mathf.PerlinNoise(px * 0.006f, pz * 0.006f) * 12.0f;
                    py = Mathf.Lerp(landH, -3f, t);
                }
                else // deep seabed
                {
                    py = -15.0f;
                }

                vertices[idx] = new Vector3((x / (float)res) * chunkCellSize, py, (z / (float)res) * chunkCellSize);
                // Tile textures nicely across rolling hills
                uv[idx] = new Vector2((x / (float)res) * 4f, (z / (float)res) * 4f);
            }
        }

        int triIdx = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int start = z * (res + 1) + x;
                triangles[triIdx++] = start;
                triangles[triIdx++] = start + res + 1;
                triangles[triIdx++] = start + 1;

                triangles[triIdx++] = start + 1;
                triangles[triIdx++] = start + res + 1;
                triangles[triIdx++] = start + res + 2;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        MeshCollider col = go.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        return go;
    }

    private void BuildRealHouse(Vector3 localPos, Transform parent)
    {
        GameObject house = new GameObject("RealHouse");
        house.transform.parent = parent;
        house.transform.localPosition = localPos;

        // Brick/Stone Walls Base with physical BoxCollider
        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "HouseWalls";
        walls.transform.parent = house.transform;
        walls.transform.localPosition = new Vector3(0.0f, 2.5f, 0.0f);
        walls.transform.localScale = new Vector3(6.5f, 5.0f, 6.5f);
        walls.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        // Terracotta Tile Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "HouseRoof";
        roof.transform.parent = house.transform;
        roof.transform.localPosition = new Vector3(0.0f, 5.8f, 0.0f);
        roof.transform.localScale = new Vector3(7.5f, 2.0f, 7.5f);
        roof.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 35.0f);
        roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
    }

    private void BuildRealBarn(Vector3 localPos, Transform parent)
    {
        GameObject barn = new GameObject("RealBarn");
        barn.transform.parent = parent;
        barn.transform.localPosition = localPos;

        // Barn main building with physical BoxCollider
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "BarnBase";
        baseObj.transform.parent = barn.transform;
        baseObj.transform.localPosition = new Vector3(0.0f, 3.5f, 0.0f);
        baseObj.transform.localScale = new Vector3(8.0f, 7.0f, 12.0f);
        baseObj.GetComponent<Renderer>().sharedMaterial = roofMaterial; // Red textured

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "BarnRoof";
        roof.transform.parent = barn.transform;
        roof.transform.localPosition = new Vector3(0.0f, 7.8f, 0.0f);
        roof.transform.localScale = new Vector3(9.0f, 2.2f, 13.0f);
        roof.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        roof.GetComponent<Renderer>().sharedMaterial = wallMaterial;
    }

    private void BuildRealTrees(Vector3 localPos, Transform parent)
    {
        int treeCount = Random.Range(2, 5);
        for (int i = 0; i < treeCount; i++)
        {
            GameObject tree = new GameObject("RealTree");
            tree.transform.parent = parent;
            Vector3 offset = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            tree.transform.localPosition = localPos + offset;

            // Trunk with physical BoxCollider
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "TreeTrunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0.0f, 2.2f, 0.0f);
            trunk.transform.localScale = new Vector3(0.6f, 2.2f, 0.6f);
            trunk.GetComponent<Renderer>().sharedMaterial = barkMaterial;

            // Textured green foliage leaves
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "TreeLeaves";
            leaves.transform.parent = tree.transform;
            leaves.transform.localPosition = new Vector3(0.0f, 5.0f, 0.0f);
            leaves.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
            leaves.GetComponent<Renderer>().sharedMaterial = leavesMaterial;
        }
    }

    private void BuildRealWindmill(Vector3 localPos, Transform parent)
    {
        GameObject windmill = new GameObject("Windmill");
        windmill.transform.parent = parent;
        windmill.transform.localPosition = localPos;

        // Brick tower base with BoxCollider
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tower.name = "WindmillTower";
        tower.transform.parent = windmill.transform;
        tower.transform.localPosition = new Vector3(0.0f, 7.5f, 0.0f);
        tower.transform.localScale = new Vector3(3.5f, 7.5f, 3.5f);
        tower.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hub.transform.parent = windmill.transform;
        hub.transform.localPosition = new Vector3(0.0f, 14.5f, 2.2f);
        hub.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        hub.GetComponent<Renderer>().sharedMaterial = barkMaterial;

        // Blades
        GameObject blades = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blades.transform.parent = hub.transform;
        blades.transform.localPosition = Vector3.zero;
        blades.transform.localScale = new Vector3(12.0f, 0.8f, 0.15f);
        blades.GetComponent<Renderer>().sharedMaterial = roadMaterial;

        GameObject bladesVertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bladesVertical.transform.parent = hub.transform;
        bladesVertical.transform.localPosition = Vector3.zero;
        bladesVertical.transform.localScale = new Vector3(0.8f, 12.0f, 0.15f);
        bladesVertical.GetComponent<Renderer>().sharedMaterial = roadMaterial;

        hub.AddComponent<SimpleRotate>().rotateSpeed = 45.0f;
    }

    private void BuildRealYacht(Vector3 localPos, Transform parent)
    {
        GameObject yacht = new GameObject("RealYacht");
        yacht.transform.parent = parent;
        yacht.transform.localPosition = localPos;
        yacht.transform.localRotation = Quaternion.Euler(0.0f, 45.0f, 0.0f);

        // White Hull
        GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.name = "YachtHull";
        hull.transform.parent = yacht.transform;
        hull.transform.localPosition = new Vector3(0.0f, 1.0f, 0.0f);
        hull.transform.localScale = new Vector3(7.5f, 2.5f, 24.0f);
        hull.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        // Wood deck - allows player to physically land and stand on the deck!
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "YachtDeck";
        deck.transform.parent = yacht.transform;
        deck.transform.localPosition = new Vector3(0.0f, 2.3f, 0.0f);
        deck.transform.localScale = new Vector3(7.0f, 0.2f, 22.0f);
        deck.GetComponent<Renderer>().sharedMaterial = barkMaterial; // Wood bark texture

        // Pilot Cabin with physical boundary colliders
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "YachtCabin";
        cabin.transform.parent = yacht.transform;
        cabin.transform.localPosition = new Vector3(0.0f, 3.6f, -3.0f);
        cabin.transform.localScale = new Vector3(5.5f, 2.6f, 11.0f);
        cabin.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        // White mast and sails
        GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.transform.parent = yacht.transform;
        mast.transform.localPosition = new Vector3(0.0f, 10.0f, 4.0f);
        mast.transform.localScale = new Vector3(0.5f, 8.5f, 0.5f);
        mast.GetComponent<Renderer>().sharedMaterial = barkMaterial;

        GameObject sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sail.transform.parent = mast.transform;
        sail.transform.localPosition = new Vector3(0.0f, 2.0f, -4.5f);
        sail.transform.localRotation = Quaternion.Euler(15.0f, 0.0f, 0.0f);
        sail.transform.localScale = new Vector3(0.05f, 10.0f, 9.0f);
        sail.GetComponent<Renderer>().sharedMaterial = wallMaterial;
    }

    private void BuildExitUI()
    {
        // Create standard Screen-Space canvas
        GameObject canvasGo = new GameObject("ExitUICanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<VRUIController>();

        // Large exit button easy for touch taps
        GameObject buttonGo = new GameObject("ExitButton");
        buttonGo.transform.parent = canvasGo.transform;

        RectTransform rt = buttonGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1.0f, 1.0f);
        rt.anchorMax = new Vector2(1.0f, 1.0f);
        rt.pivot = new Vector2(1.0f, 1.0f);
        rt.anchoredPosition = new Vector3(-25.0f, -25.0f, 0.0f);
        rt.sizeDelta = new Vector2(90.0f, 90.0f); // 90x90 Generous touch boundary

        Image btnImg = buttonGo.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.08f, 0.08f, 0.82f); // Vibrant semitransparent red

        buttonGo.AddComponent<Button>();

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
        text.fontSize = 50;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private Texture2D LoadPNG(string filename)
    {
        string path = Path.Combine(Application.dataPath, "Textures", filename);
        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                return tex;
            }
        }
        return null;
    }

    private Material CreateStandardMaterial(Texture2D tex, Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        mat.name = name;
        if (tex != null)
        {
            mat.mainTexture = tex;
        }
        else
        {
            mat.color = color;
        }

        if (shader.name == "Standard" || shader.name.Contains("Simple Lit"))
        {
            mat.SetFloat("_Glossiness", 0.0f); // default low gloss
        }
        return mat;
    }

    private Material CreateTransparentMaterial(Texture2D tex, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        bool isURP = (shader != null);
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");

        Material mat = new Material(shader);
        mat.name = name;
        mat.mainTexture = tex;

        if (isURP)
        {
            // Setup URP transparent rendering
            mat.SetFloat("_Blend", 0.0f); // Alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (shader.name == "Standard")
        {
            // Setup Standard transparent rendering
            mat.SetFloat("_Mode", 3.0f); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        return mat;
    }
}

public class SimpleRotate : MonoBehaviour
{
    public float rotateSpeed = 45.0f;

    void Update()
    {
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime, Space.Self);
    }
}
