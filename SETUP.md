# VR Pegasus Flying Game Setup Guide

Welcome to your **VR Pegasus Flying Game**! This Unity project has been initialized and structured for Unity version **2022.3.23f1** and is optimized for low-end Android devices (like the Samsung Galaxy A01).

To get you flying instantly with a premium, stylized look and zero asset-importing hassle, we have built a **Procedural Environment Generator** that sets up the entire scene dynamically.

---

## 🚀 Quick Start Instructions

Follow these 4 simple steps to open, generate, and run the game:

### Step 1: Open the Project in Unity
1. Launch **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select the folder: `c:\Users\beaum\Desktop\exercisevr`.
4. Open the project. Unity will automatically download and install the required packages (URP, Google Cardboard XR, etc.) which might take 1–2 minutes on first load.

### Step 2: Create and Generate the Scene
1. Inside the Unity Editor, create a new empty scene (**File** -> **New Scene** -> select **Empty (Built-in)** or **Empty**).
2. Save the scene (**File** -> **Save As...**) inside the `Assets` folder as `MainScene.unity`.
3. In the Hierarchy, right-click and choose **Create Empty**. Name this GameObject `GameManager`.
4. With `GameManager` selected, drag and drop the `EnvironmentBuilder` script (located in `Assets/Scripts/`) onto it in the Inspector.
5. In the Inspector, right-click the **Environment Builder** component header (the three dots `⋮` on the top-right of the component) and select **Build Scene**.
   - *Boom!* The script will instantly generate the entire world in your Editor: the stylized farmland grid, windmill structures, red barns, trees, ocean surface, the yacht, the Pegasus model with its wing flapping rig, the stereoscopic camera rig, and the Exit UI Canvas!
6. Save the scene again.

### Step 3: Test in the Unity Editor
1. Click the **Play** button at the top of the Unity Editor.
2. **Move around**: Hold down the **Space Bar** or **W key** to simulate jogging/walking in place. The Pegasus will smoothly fly forward and up!
3. **Look around**: Hold down the **Right Mouse Button** and drag to simulate looking around in the Cardboard headset.
4. **Steer Left/Right (Tilt)**: Press **Q** (tilt head left) or **E** (tilt head right) while walking to smoothly steer the Pegasus left and right 180 degrees!
5. **Exit**: Click the red **X** button in the upper-right corner of the screen to stop the game.

### Step 4: Build to your Samsung Galaxy A01
1. Connect your Samsung Galaxy A01 to your PC via USB and ensure **Developer Mode / USB Debugging** is enabled on the phone.
2. In Unity, open **File** -> **Build Settings**.
3. Switch the platform to **Android** and click **Switch Platform**.
4. Under **Scenes in Build**, click **Add Open Scenes** to add your `MainScene`.
5. Open **Player Settings** (bottom-left of Build Settings):
   - **XR Plug-in Management**: Check **Cardboard XR Plugin** under the Android tab. (This will activate the native split-screen VR viewer).
   - **Resolution and Presentation**: Ensure the Default Orientation is set to **Landscape Left**.
   - **Other Settings**:
     - Set **Graphics APIs** to **OpenGLES3** (remove Vulkan if present, as OpenGLES3 runs more stably on the Galaxy A01).
     - Set **Minimum API Level** to Android 8.0 (API Level 26) or higher.
6. Click **Build and Run**, name your APK, and Unity will compile and install the game directly onto your Galaxy A01!

---

## 🎮 How the Mechanics Work (Code Structure)

Here is a quick look at the custom C# scripts we created for you in `Assets/Scripts/`:

1. **[VRMotionController.cs](file:///c:/Users/beaum/Desktop/exercisevr/Assets/Scripts/VRMotionController.cs)**:
   - **Accelerometer Walking (Step Detector)**: Continually measures dynamic G-force fluctuations. If the vertical bounce amplitude exceeds `0.12G` (representing an up-and-down movement of 0.5 inches or more from jogging/walking in place), it triggers/sustains forward and upward flight.
   - **Steering by Head Tilt (Roll)**: Reads the camera rig's Z-axis rotation. If you tilt your head left or right, it steers the Pegasus yaw (horizontal angle) smoothly in that direction up to a full 180 degrees.
   - **Fallback Gyro Tracking**: If the Cardboard XR module is not running, it reads the phone's native gyroscope directly and rotates the cameras in real-time, providing stereoscopic tracking out-of-the-box.
2. **[EnvironmentBuilder.cs](file:///c:/Users/beaum/Desktop/exercisevr/Assets/Scripts/EnvironmentBuilder.cs)**:
   - Sets up directional warm lighting.
   - Generates a vibrant patchwork of farmland fields (green, yellow, brown) scattered with red barns, green foliage trees, and working rotating windmills.
   - Creates a deep-blue ocean on the right side of the landscape, complete with a detailed white yacht and sail.
   - Models a stylized low-poly Pegasus (body, head, eyes, ears, legs, tail, and flapping wings).
   - Instantiates a stereoscopic dual-camera rig positioned perfectly as if riding on the back of the Pegasus.
3. **[PegasusAnimator.cs](file:///c:/Users/beaum/Desktop/exercisevr/Assets/Scripts/PegasusAnimator.cs)**:
   - Controls wing rotation. When walking in place, the wings flap rapidly. When you stop, the wings settle into a slow, elegant idle glide.
4. **[VRUIController.cs](file:///c:/Users/beaum/Desktop/exercisevr/Assets/Scripts/VRUIController.cs)**:
   - Listens to the exit canvas screen tap. Clicking the top-right **X** exits the app cleanly, allowing the user to remove the headset and safely close the app.
