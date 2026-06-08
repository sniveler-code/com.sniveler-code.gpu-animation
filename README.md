<div align="center">

# 🚀 GPU Animation Entities LITE

**High-performance crowd animation system for Unity DOTS / ECS.**

[![Unity](https://img.shields.io/badge/Unity-2022.2%2B%20%7C%206-black?style=flat-square&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Pipeline-URP-blue?style=flat-square)](#)
[![ECS](https://img.shields.io/badge/Architecture-DOTS-brightgreen?style=flat-square)](#)

*Render thousands of animated units at maximum FPS with zero CPU bottlenecks.*

</div>

---

Built specifically for the Universal Render Pipeline (URP), this asset bypasses traditional animation overhead by baking skeletal data into highly optimized `BlobAsset`s and performing matrix skinning directly inside the Vertex Shader.

> **Note**
> This is the **FREE LITE version** of our premium crowd animation tool. It is perfect for testing the performance of DOTS-based GPU animation in your project, participating in game jams, or building simple swarm-survival games.

## ✨ Features of the LITE Version

* ⚡ **ECS-Native Performance:** Bypass the traditional `Animator` component. Our system uses `EntityCommandBuffer` and Burst-compiled jobs to feed animation data to the GPU, allowing you to render massive crowds with minimal overhead.
* 🎨 **Shader Graph Support:** Don't limit yourself to basic shaders. We provide out-of-the-box Shader Graph templates. Add dissolve effects, cel-shaded outlines, or custom emission logic to your animated crowds in seconds using our custom Animation Node.
* ⚙️ **One-Click Automated Baking:** No complex setup. Drag your standard Unity GameObject (with a `SkinnedMeshRenderer` and `Animator`) into our custom Editor Window, click "Bake", and receive a fully configured DOTS Prefab ready to be spawned via ECS.

## ⚙️ Installation & Setup

Follow these steps to get **GPU Animation Entities LITE** running in your project.

**1. Install Required Packages**
Ensure your project is using the Universal Render Pipeline (URP) and has the necessary DOTS packages installed. Open the Package Manager (**Window > Package Manager**), click **+ > Add package by name...** and add:
* `com.unity.entities`
* `com.unity.entities.graphics`
* `com.unity.burst`

**2. Import the Asset**
Import the GPU Animation Entities LITE package into your Unity project.

**3. Enable Burst Compilation**
For maximum performance, ensure Burst is enabled.
Go to **Jobs > Burst > Enable**.
*(Ensure Safety Checks are turned off in production builds for optimal speed).*

**4. Verify URP Settings**
Ensure your project is actively using a URP Asset (**Edit > Project Settings > Graphics**). The custom shaders provided in this package will only compile under URP.

---

## 🚀 Quick Start Guide (Your First Crowd)

Let's bake your first character and spawn a massive crowd!

### Step 1: Prepare Your Model
1. Import your 3D character (FBX) into Unity and place it in the scene.
2. Ensure it has a `SkinnedMeshRenderer` and an `Animator` component.
3. Assign an `AnimatorController` with animations (e.g., Idle, Walk, Attack).

### Step 2: Bake the Animation Data
1. Open the Baker Window via **Window > Sniveler Code > Animator Baker**.
2. Drag and drop your character from the scene into the **Prefab Model** field. The window will automatically detect your Animator.
3. Select a Shader (e.g., `Sniveler Lit`).
4. Click **Process**.

The system will generate a new folder at `Assets/SnivelerCode.GpuAnimation.Generated/[YourCharacterName]` containing the baked DOTS Prefab, Material, and `BlobAsset`s.

---

## 🎛️ The Animator Baker Window

The **Animator Baker** is the core tool for converting standard Unity animations into highly optimized GPU data. Open it via: **Window > Sniveler Code > Animator Baker**.

### 1. Prefab Model
Drag your source GameObject here. It must contain a `SkinnedMeshRenderer`.

### 2. Animator Settings
Once a valid prefab is assigned, the Animator settings will appear.
* **Animations List:** You will see a list of states from your Animator Controller. You can adjust the baking FPS for each.

### 3. Shader Selection
Choose the material template for your baked mesh:
* **Sniveler Lit:** Standard URP Lit shading.
* **Sniveler Unlit:** Cheap, unlit shading for maximum performance.
* **Custom:** Provide your own Shader Graph (must include the `SnivelerAnimationNode`).

### 4. PRO Features (Disabled in LITE)
You will notice several disabled toggles and tabs in the UI:
* 🔒 **Use Dual Quaternion:** Prevents mesh collapsing on joints.
* 🔒 **Apply Root Motion:** Bakes movement data into the entity transform.
* 🔒 **Bones Tab:** Exposes specific bones for attaching weapons/items.

*These features are fully unlocked in **[GPU Animation Entities PRO](https://assetstore.unity.com/packages/tools/animation/gpu-animation-entities-pro-370150)**.*

---

## 🎨 Shader Graph Integration

You can easily create custom materials for your GPU-animated characters using Unity's Shader Graph.

### Using the Custom Node
1. Create a new URP Shader Graph (Lit or Unlit).
2. Right-click in the graph and create a **Custom Function** node.
3. We provide a ready-to-use SubGraph called `SnivelerAnimationNode`. Simply drag and drop it into your Shader Graph.
4. Connect the **Position**, **Normal**, and **Tangent** outputs of the SubGraph to the **Vertex** section of your Master Node.

### How it works
The `SnivelerAnimationNode` reads the current animation frame from the entity's material properties and fetches the correct bone matrices from a global `StructuredBuffer`. It then performs Linear Blend Skinning (LBS) directly in the vertex shader.

> 👑 **PRO Tip:** The PRO version includes **Dual Quaternion Skinning (DQS)** inside this node, which completely eliminates the "candy-wrapper" artifact on twisting joints (like shoulders and wrists).

---

## 💻 Runtime Control (C# ECS API)

Controlling animations at runtime is done by modifying the `AnimatorData` component on your entities.

### Changing Animations
To change an animation, simply update the `Index` field of the `AnimatorData` component.

```csharp
using Unity.Entities;
using SnivelerCode.GpuAnimation.Runtime.Components;

public partial struct PlayAnimationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Example: Set all entities to play Animation Index 1
        foreach (var animator in SystemAPI.Query<RefRW<AnimatorData>>())
        {
            // Instantly snap to the new animation
            animator.ValueRW.Index = 1;
            animator.ValueRW.Time = 0f; 
            animator.ValueRW.Frame = 0;
        }
    }
}
```

### Animation Snapping vs. Crossfading
In the LITE version, changing the Index results in an immediate "snap" to the new animation.

> 👑 **PRO Tip:** In the PRO version, you can use the `.Play(index, crossfadeTime)` extension method to smoothly blend between animations using hardware-accelerated transition weights!

---

## 📦 What's Included in LITE?

* **The Animator Baker Window (Lite):** A visual editor for extracting animation data into DOTS `BlobAsset`s.
* **Shader Graph Templates:** Ready-to-use Lit and Unlit templates with DOTS animation logic pre-wired.
* **Demo Scene:** A basic stress-test scene demonstrating how to spawn and animate a massive crowd using ECS.

## 👑 Upgrade to PRO: The Ultimate AAA Toolset

Loved the performance but need more features for a full-scale game? **[GPU Animation Entities PRO](https://assetstore.unity.com/packages/tools/animation/gpu-animation-entities-pro-370150)** unlocks the ultimate toolset for your DOTS project:

| Feature | LITE | PRO |
| :--- | :---: | :--- |
| **Zero-Latency Sockets** | ❌ | ✅ Attach swords/VFX with frame-perfect precision |
| **Dual Quaternion Skinning** | ❌ | ✅ Preserves mesh volume, prevents "candy-wrapper" |
| **True Root Motion** | ❌ | ✅ Fully compatible with DOTS physics |
| **Smooth Transitions** | ❌ | ✅ Smooth crossfading between animation states |
| **Tick Rate Optimization** | ❌ | ✅ Reduce update frequency for distant units |

👉 **[Get GPU Animation Entities PRO on the Asset Store](https://assetstore.unity.com/packages/tools/animation/gpu-animation-entities-pro-370150)**

## 🛠️ Technical Requirements

| Requirement | Supported Version(s) |
| :--- | :--- |
| **Unity Version** | `2022.2+` *(Unity 6 Supported)* |
| **Render Pipeline** | `URP 14.0+` *(HDRP/Built-in not supported)* |
| **Entities (DOTS)** | `1.4.5+` |
| **Entities Graphics** | `1.4.18+` |
| **Burst Compiler** | `1.8.27+` |

> [!IMPORTANT]
> **ECS Paradigm Only**
> This asset relies strictly on the ECS paradigm. It does not use traditional `GameObject`s for runtime rendering. You must be familiar with spawning and managing entities via `EntityCommandBuffer` or Bakers.

## 📚 Documentation & Support

We believe in zero-friction development. Comprehensive documentation is available to guide you from your first FBX to your first massive crowd.

📖 **[Read the Full Online Documentation Here](https://sniveler-code.gitbook.io/dots/gpu-animation-entities-pro)**

### Contact Us
* 📧 **Email:** [sniveler.code@gmail.com](mailto:sniveler.code@gmail.com)
* 🌐 **GitHub:** [@sniveler-code](https://github.com/sniveler-code)

---
<div align="center">
<i>Developed with ❤️ for high-performance Unity developers.</i>
</div>