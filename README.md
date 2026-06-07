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

## 📦 What's Included in LITE?

* **The Animator Baker Window (Lite):** A visual editor for extracting animation data into DOTS `BlobAsset`s.
* **Shader Graph Templates:** Ready-to-use Lit and Unlit templates with DOTS animation logic pre-wired.
* **Demo Scene:** A basic stress-test scene demonstrating how to spawn and animate a massive crowd using ECS.

## 👑 Upgrade to PRO: The Ultimate AAA Toolset

Loved the performance but need more features for a full-scale game? **[GPU Animation Entities PRO](#)** unlocks the ultimate toolset for your DOTS project:

| Feature | LITE | PRO |
| :--- | :---: | :--- |
| **Zero-Latency Sockets** | ❌ | ✅ Attach swords/VFX with frame-perfect precision |
| **Dual Quaternion Skinning** | ❌ | ✅ Preserves mesh volume, prevents "candy-wrapper" |
| **True Root Motion** | ❌ | ✅ Fully compatible with DOTS physics |
| **Smooth Transitions** | ❌ | ✅ Smooth crossfading between animation states |
| **Tick Rate Optimization** | ❌ | ✅ Reduce update frequency for distant units |

👉 **[Get GPU Animation Entities PRO on the Asset Store](#https://assetstore.unity.com/packages/tools/animation/gpu-animation-entities-pro-370150)**

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

## 📚 Support

* 📧 **Email:** [sniveler.code@gmail.com](mailto:sniveler.code@gmail.com)
* 🌐 **GitHub:** [@sniveler-code](https://github.com/sniveler-code)

---
<div align="center">
<i>Developed with ❤️ for high-performance Unity developers.</i>
</div>