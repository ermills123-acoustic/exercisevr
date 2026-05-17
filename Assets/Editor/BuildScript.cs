using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void PerformBuild()
    {
        Debug.Log("Starting Automated VR Pegasus Build...");

        // 1. Create a new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene";

        // 2. Spawn EnvironmentBuilder and set it to generate dynamically at runtime
        GameObject gm = new GameObject("GameManager");
        EnvironmentBuilder builder = gm.AddComponent<EnvironmentBuilder>();
        builder.generateOnStart = true; // Dynamically build at runtime on the device for flawless asset serialization!

        // Load and assign textures via AssetDatabase so they are permanently serialized in the scene GameManager component
        builder.farmlandTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/farmland_texture.png");
        builder.oceanTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/ocean_texture.png");
        builder.roadTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/road_texture.png");
        builder.wallTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/house_wall.png");
        builder.roofTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/house_roof.png");
        builder.barkTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/bark_texture.png");

        // 3. Save the scene
        string scenePath = "Assets/Scenes/MainScene.unity";
        
        // Ensure Scenes folder exists
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        }

        bool saveSuccess = EditorSceneManager.SaveScene(scene, scenePath);
        if (!saveSuccess)
        {
            Debug.LogError("Failed to save the generated scene!");
            return;
        }
        Debug.Log($"Scene successfully generated and saved to {scenePath}");

        // 4. Setup build scenes
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene(scenePath, true)
        };

        // 5. Ensure target platform is Android
        BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (activeTarget != BuildTarget.Android)
        {
            Debug.Log("Switching build target to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        // 6. Set Player Settings
        PlayerSettings.companyName = "EddieMills";
        PlayerSettings.productName = "PegasusVR";
        PlayerSettings.applicationIdentifier = "com.eddiemills.pegasusvr";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto; // Auto detect highest installed SDK

        
        // Disable Auto Graphics API so Unity respects our GLES3 selection and compiles without Vulkan (preventing Google Cardboard crash on start!)
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] {
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
        });

        // Set target architectures to both ARMv7 (32-bit for Galaxy A01) and ARM64 (64-bit for compatibility)
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        // 7. Define Output Path
        string buildFolder = "Builds";
        if (!System.IO.Directory.Exists(buildFolder))
        {
            System.IO.Directory.CreateDirectory(buildFolder);
        }
        string apkPath = $"{buildFolder}/PegasusVR.apk";

        // 8. Build the APK
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[] { scenePath };
        buildPlayerOptions.locationPathName = apkPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log($"Building APK at path: {apkPath}...");
        
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded! File size: {summary.totalSize} bytes. APK saved to {apkPath}");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"Build failed! Total errors: {summary.totalErrors}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                    {
                        Debug.LogError($"Build Error: {msg.content}");
                    }
                }
            }
            throw new System.Exception("Unity APK Build Failed!");
        }
    }
}
