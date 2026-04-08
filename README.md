# GPU Uniform Grid for Unity

UPM package **`jp.nobnak.gpu_uniform_grid`**: build a **uniform spatial grid on the GPU in one pass**, following *Fast Uniform Grid Construction on GPGPUs Using Atomic Operations* [^1].

Cells use **3D Morton codes**; each cell holds a **linked list** of element IDs (`cellHead` + `cellNext`). Built-in compute clears buffers; your kernels insert with atomics. A **2D Morton variant** (`UniformGrid2D`, `CPUUniformGrid2D`) is included for planar / 2D workflows.

**API (short)**

| Piece | Role |
|--------|------|
| `UniformGridParams` | Center, `gridSize`, `bitsPerAxis` (≤ `MaxSupportedBitsPerAxis`), `elementCapacity` |
| `GPUUniformGrid` | Buffers, `Reset()`, `SetParams` / globals for compute & shaders |
| HLSL | [`UniformGrid.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid.hlsl), [`UniformGrid2D.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid2D.hlsl), [`UniformGrid-hl.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl) (`InsertElementIdAtPosition`, `GetParticleDensityAtPosition` + `GET_PARTICLE_POSITION`) |
| `UniformGridConverter.ToCPU` | GPU → [`CPUUniformGrid`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/CPUUniformGrid.cs) (debug) |
| `UniformGridView` | Optional cell visualization ([`Runtime/View`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/View)) |

**Dependency:** `com.unity.mathematics`. Core package is **render-pipeline agnostic**; this repo’s samples target **URP**.

---

## Install

**[OpenUPM — jp.nobnak.gpu_uniform_grid](https://openupm.com/packages/jp.nobnak.gpu_uniform_grid/)**

```bash
openupm add jp.nobnak.gpu_uniform_grid
```

Or scoped registry `https://package.openupm.com`, scope `jp.nobnak`, then install in Package Manager. Pin version in `manifest.json` to match the release you want.

---

## Usage

1. Construct `UniformGridParams` and `new GPUUniformGrid(params)`; `Dispose()` when done.
2. Each frame: `Reset()`, then run your insert pass after `SetParams(compute, kernel)` or `SetParamsGlobal()`.
3. In HLSL, call `InsertElementIdAtPosition` or `UniformGrid_InsertElementIDAtCellID`.

---

## Samples

**In this repository** (authoring copy under `Assets/Samples`):

| Folder | Content |
|--------|---------|
| [`Assets/Samples/UniformGrid`](Assets/Samples/UniformGrid) | CPU proximity (3D/2D), shared includes, `Scenes` for grid views |
| [`Assets/Samples/Particles`](Assets/Samples/Particles) | [`ParticleDataUploader.cs`](Assets/Samples/Particles/ParticleDataUploader.cs), compute upload, particle shading |

**From an installed package:** Package Manager → **GPU Uniform Grid** → **Samples** → import **Uniform Grid — CPU proximity & views** (`Samples~/UniformGrid`) or **Uniform Grid — GPU particles** (`Samples~/Particles`). Unity hides `Samples~` from the Project window; imported files appear under `Assets` as usual.

**Maintainers (this repo):** [`Assets/Editor/PackageSampleEmbed.cs`](Assets/Editor/PackageSampleEmbed.cs) mirrors `Assets/Samples` into [`Packages/jp.nobnak.gpu_uniform_grid/Samples~`](Packages/jp.nobnak.gpu_uniform_grid/Samples~) (same layout as `package.json` sample paths) on compile / project change, or via **Tools → GPU Uniform Grid → Force Copy Samples to Package (Samples~)**. Run a force copy before packing or releasing so tarball/registry consumers see up-to-date samples.

---

## Demo videos

[![Dense particle highlighting](http://img.youtube.com/vi/GRpFk6DCQ8U/mqdefault.jpg)](https://youtube.com/shorts/GRpFk6DCQ8U) · [![Dense grid visualization](http://img.youtube.com/vi/GsY-AYIolQ8/mqdefault.jpg)](https://youtube.com/shorts/GsY-AYIolQ8) · [![Active cells](http://img.youtube.com/vi/NKYRA955oSE/mqdefault.jpg)](https://youtu.be/NKYRA955oSE) · [![Variable grid size](http://img.youtube.com/vi/8GmqgaxiQ2g/mqdefault.jpg)](https://youtu.be/8GmqgaxiQ2g)

---

## References

[^1]: Davide Barbieri, Valeria Cardellini, and Salvatore Filippone. 2013. *Fast Uniform Grid Construction on GPGPUs Using Atomic Operations.* International Conference on Parallel Computing (2013). https://doi.org/10.3233/978-1-61499-381-0-295
