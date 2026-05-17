using UnityEditor.Android;
using System.IO;
using UnityEngine;

public class GradlePostProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log($"[GradlePostProcessor] OnPostGenerateGradleAndroidProject called with path: {path}");

        string parentDir = Path.GetDirectoryName(path);

        // Modify gradle.properties to enable AndroidX and Jetifier
        string gradlePropertiesPath = Path.Combine(parentDir, "gradle.properties");
        if (File.Exists(gradlePropertiesPath))
        {
            Debug.Log("[GradlePostProcessor] Modifying gradle.properties to enable AndroidX and Jetifier...");
            string contents = File.ReadAllText(gradlePropertiesPath);
            if (!contents.Contains("android.useAndroidX"))
            {
                contents += "\nandroid.useAndroidX=true\nandroid.enableJetifier=true\n";
                File.WriteAllText(gradlePropertiesPath, contents);
                Debug.Log("[GradlePostProcessor] Successfully enabled AndroidX and Jetifier in gradle.properties!");
            }
            else
            {
                Debug.Log("[GradlePostProcessor] android.useAndroidX is already present in gradle.properties.");
            }
        }
        else
        {
            Debug.LogError($"[GradlePostProcessor] gradle.properties not found at: {gradlePropertiesPath}");
        }

        // Modify unityLibrary/build.gradle
        string unityLibraryGradlePath = Path.Combine(path, "build.gradle");
        if (File.Exists(unityLibraryGradlePath))
        {
            string contents = File.ReadAllText(unityLibraryGradlePath);
            
            // Check if appcompat is already there
            if (!contents.Contains("androidx.appcompat:appcompat"))
            {
                Debug.Log("[GradlePostProcessor] Adding androidx.appcompat:appcompat to unityLibrary/build.gradle...");
                // Insert implementation 'androidx.appcompat:appcompat:1.6.1' under the GfxPluginCardboard dependency
                string target = "implementation(name: 'GfxPluginCardboard', ext:'aar')";
                string replacement = target + "\n    implementation 'androidx.appcompat:appcompat:1.6.1'";
                
                if (contents.Contains(target))
                {
                    contents = contents.Replace(target, replacement);
                    File.WriteAllText(unityLibraryGradlePath, contents);
                    Debug.Log("[GradlePostProcessor] Successfully modified unityLibrary/build.gradle!");
                }
                else
                {
                    Debug.LogWarning("[GradlePostProcessor] GfxPluginCardboard dependency not found in build.gradle. Trying fallback insertion...");
                    // Fallback: insert at the end of the dependencies block
                    int depIndex = contents.IndexOf("dependencies {");
                    if (depIndex != -1)
                    {
                        int insertIndex = contents.IndexOf("}", depIndex);
                        if (insertIndex != -1)
                        {
                            contents = contents.Insert(insertIndex, "    implementation 'androidx.appcompat:appcompat:1.6.1'\n");
                            File.WriteAllText(unityLibraryGradlePath, contents);
                            Debug.Log("[GradlePostProcessor] Fallback modification of unityLibrary/build.gradle succeeded!");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("[GradlePostProcessor] androidx.appcompat:appcompat is already present in unityLibrary/build.gradle.");
            }
        }
        else
        {
            Debug.LogError($"[GradlePostProcessor] unityLibrary build.gradle not found at: {unityLibraryGradlePath}");
        }

        // Modify launcher/build.gradle for safety
        string launcherGradlePath = Path.Combine(parentDir, "launcher", "build.gradle");
        if (File.Exists(launcherGradlePath))
        {
            string contents = File.ReadAllText(launcherGradlePath);
            if (!contents.Contains("androidx.appcompat:appcompat"))
            {
                Debug.Log("[GradlePostProcessor] Adding androidx.appcompat:appcompat to launcher/build.gradle...");
                string target = "implementation project(':unityLibrary')";
                string replacement = target + "\n    implementation 'androidx.appcompat:appcompat:1.6.1'";
                if (contents.Contains(target))
                {
                    contents = contents.Replace(target, replacement);
                    File.WriteAllText(launcherGradlePath, contents);
                    Debug.Log("[GradlePostProcessor] Successfully modified launcher/build.gradle!");
                }
            }
        }
    }
}
