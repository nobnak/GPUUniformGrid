# GPU Uniform Grid for Unity

UPM package **`jp.nobnak.gpu_uniform_grid`**: a **GPU uniform spatial grid** built in a single pass, following Barbieri et al. [^1]. Each cell stores a **linked list** of element IDs (`cellHead`, `cellNext`). Package **compute shaders** clear buffers; **your** kernels insert with atomics.

**Encoding:** **3D** cells use a **Morton `uint`** per cell (`UniformGridParams`, **`bitsPerAxis` ≤ 10**). **2D** uses a separate encoding with **`UniformGridParams2D`** (**≤ 16 bits per axis**). Pick **`GPUUniformGrid`** / **`GPUUniformGrid2D`** and matching HLSL (`UniformGrid*.hlsl`, [`UniformGridLinkedList.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGridLinkedList.hlsl)).

| Area | Main types / assets |
|------|---------------------|
| 3D grid | [`UniformGridParams`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Data/UniformGridParams.cs), [`GPUUniformGrid`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/GPUUniformGrid.cs), [`CPUUniformGrid`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/CPUUniformGrid.cs), [`UniformGrid.compute`](Packages/jp.nobnak.gpu_uniform_grid/Resources/Shader/UniformGrid.compute) |
| 2D grid | [`UniformGridParams2D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Data/UniformGridParams2D.cs), [`GPUUniformGrid2D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/GPUUniformGrid2D.cs), [`CPUUniformGrid2D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/CPUUniformGrid2D.cs), [`UniformGrid2D.compute`](Packages/jp.nobnak.gpu_uniform_grid/Resources/Shader/UniformGrid2D.compute) |
| Compact 3D / 2D | [`CompactUniformGridParams3D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Data/CompactUniformGridParams3D.cs) / [`CompactUniformGridParams2D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Data/CompactUniformGridParams2D.cs), [`GPUCompactUniformGrid`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Compact/GPUCompactUniformGrid.cs) / [`GPUCompactUniformGrid2D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Compact/GPUCompactUniformGrid2D.cs), compute under [`Resources/Shader/Compact/`](Packages/jp.nobnak.gpu_uniform_grid/Resources/Shader/Compact/) — [`Docs/unity_fixed_grid.md`](Docs/unity_fixed_grid.md) |
| HLSL helpers | [`UniformGrid-hl.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl), [`UniformGrid2D-hl.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid2D-hl.hlsl) — e.g. `InsertElementIdAtPosition`, `GetParticleDensityAtPosition` (define **`GET_PARTICLE_POSITION`**) |
| CPU readback | [`UniformGridConverter.ToCPU`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/Converter/UniformGridConverter.cs) → `CPUUniformGrid` / `CPUUniformGrid2D` |
| Debug draw | [`UniformGridView`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/View/UniformGridView.cs), [`UniformGridView2D`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/View/UniformGridView2D.cs) + package shaders |

**Dependency:** `com.unity.mathematics`. The library does not depend on a specific render pipeline; **samples in this repo use URP**.

---

## Install

**[OpenUPM — jp.nobnak.gpu_uniform_grid](https://openupm.com/packages/jp.nobnak.gpu_uniform_grid/)**

```bash
openupm add jp.nobnak.gpu_uniform_grid
```

Or add the OpenUPM scoped registry (`https://package.openupm.com`, scope `jp.nobnak`) and install from Package Manager. Pin the version in `manifest.json` (e.g. `"jp.nobnak.gpu_uniform_grid": "1.8.2"`) to match the release you use.

---

## Usage (outline)

**3D:** `new GPUUniformGrid(params)` → each frame `Reset()` → `SetParams(compute, kernel)` (or globals) on your insert pass → in HLSL use helpers from `UniformGrid-hl.hlsl` / low-level APIs in `UniformGrid.hlsl`. **`Dispose()`** when done.

**2D:** Same flow with `GPUUniformGrid2D` / `UniformGrid2D-hl.hlsl` / `UniformGrid2D.hlsl`.

**Compact (per-cell cap `M`):** `GPUCompactUniformGrid2D` / `GPUCompactUniformGrid` → each frame `Reset()` then `DispatchBuild(positions, count)`; optional `CompactUniformGridOverflowDebug`. See package README.

---

## Samples

| Location | What |
|----------|------|
| [`Assets/Samples/UniformGrid`](Assets/Samples/UniformGrid) | CPU proximity queries (**`Proximity3D`**, **`Proximity2D`**), shared includes under **`Include/`** |
| [`Assets/Samples/Particles`](Assets/Samples/Particles) | GPU particle upload ([`ParticleDataUploader.cs`](Assets/Samples/Particles/ParticleDataUploader.cs)), particle / density shaders, and **`UniformGridView.unity`** (grid visualization scene) |

**Consumers of the package only:** Package Manager → **GPU Uniform Grid** → **Samples** → import **`Samples~/UniformGrid`** or **`Samples~/Particles`** (Unity hides **`Samples~`** in the Project window).

**This repository:** [`Assets/Editor/PackageSampleEmbed.cs`](Assets/Editor/PackageSampleEmbed.cs) copies **`Assets/Samples`** → [`Packages/jp.nobnak.gpu_uniform_grid/Samples~`](Packages/jp.nobnak.gpu_uniform_grid/Samples~) and **[`README.md`](README.md)** → [`Packages/jp.nobnak.gpu_uniform_grid/README.md`](Packages/jp.nobnak.gpu_uniform_grid/README.md) (for UPM / registry documentation), on relevant editor events or via **Tools → GPU Uniform Grid → Force Copy Samples & README to Package**. Run before release if samples or docs changed.

---

## Demo videos

[![Dense particle highlighting](http://img.youtube.com/vi/GRpFk6DCQ8U/mqdefault.jpg)](https://youtube.com/shorts/GRpFk6DCQ8U) · [![Dense grid visualization](http://img.youtube.com/vi/GsY-AYIolQ8/mqdefault.jpg)](https://youtube.com/shorts/GsY-AYIolQ8) · [![Active cells](http://img.youtube.com/vi/NKYRA955oSE/mqdefault.jpg)](https://youtu.be/NKYRA955oSE) · [![Variable grid size](http://img.youtube.com/vi/8GmqgaxiQ2g/mqdefault.jpg)](https://youtu.be/8GmqgaxiQ2g)

---

## References

[^1]: Davide Barbieri, Valeria Cardellini, and Salvatore Filippone. 2013. *Fast Uniform Grid Construction on GPGPUs Using Atomic Operations.* International Conference on Parallel Computing (2013). https://doi.org/10.3233/978-1-61499-381-0-295
