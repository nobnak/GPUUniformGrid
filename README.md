# GPU Uniform Grid for Unity

**GPU Uniform Grid** is a Unity UPM package (`jp.nobnak.gpu_uniform_grid`) that builds a **uniform spatial grid on the GPU in a single pass**, following the approach in *Fast Uniform Grid Construction on GPGPUs Using Atomic Operations* [^1].

Cells are addressed with **3D Morton codes**. Each cell stores a **linked list** of element IDs: a `cellHead` buffer (first ID per cell) and a `cellNext` buffer (per-element chain). `GPUUniformGrid` dispatches built-in compute kernels to clear buffers; insertion uses atomic operations in your shaders.

**What you get**

- **`UniformGridParams`** — Grid center, cube side length `gridSize`, `bitsPerAxis` so each axis has `2^bitsPerAxis` cells, and `elementCapacity`. Cell IDs are stored in a **single `uint` Morton code**, so **`bitsPerAxis` must not exceed `UniformGridParams.MaxSupportedBitsPerAxis` (10)** without extending the encoding.
- **`GPUUniformGrid`** — Owns `cellHead` / `cellNext` `GraphicsBuffer`s, `Reset()`, and `SetParams` for a **ComputeShader kernel**, **MaterialPropertyBlock**, or **global shader** properties.
- **HLSL** — Low-level helpers in [`UniformGrid.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid.hlsl); higher-level helpers in [`UniformGrid-hl.hlsl`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl), including [`InsertElementIdAtPosition`](Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl) and `GetParticleDensityAtPosition` (requires defining `GET_PARTICLE_POSITION`).
- **`UniformGridConverter.ToCPU`** — Copies the GPU grid to a [`CPUUniformGrid`](Packages/jp.nobnak.gpu_uniform_grid/Runtime/CPUUniformGrid.cs) via `AsyncGPUReadback` for debugging.
- **`UniformGridView`** — Optional grid / cell-density visualization (`Runtime/View`).

**Dependency:** `com.unity.mathematics`. The core package is **not tied to a specific render pipeline** (the sample project may use URP).

---

## Demo videos

[![Dense grid visualization](http://img.youtube.com/vi/GRpFk6DCQ8U/mqdefault.jpg)](https://youtube.com/shorts/GRpFk6DCQ8U)  
Dense particle highlighting

[![Dense grid visualization](http://img.youtube.com/vi/GsY-AYIolQ8/mqdefault.jpg)](https://youtube.com/shorts/GsY-AYIolQ8)  
Dense grid visualization

[![Thumbnail](http://img.youtube.com/vi/NKYRA955oSE/mqdefault.jpg)](https://youtu.be/NKYRA955oSE)  
Close look at active cells

[![Thumbnail](http://img.youtube.com/vi/8GmqgaxiQ2g/mqdefault.jpg)](https://youtu.be/8GmqgaxiQ2g)  
Variable grid size

---

## Install (OpenUPM)

Package registry page: **[jp.nobnak.gpu_uniform_grid](https://openupm.com/packages/jp.nobnak.gpu_uniform_grid/)**

Same pattern as **[Circle Renderer (URP) — OpenUPM readme](https://openupm.com/packages/jp.nobnak.circle/?subPage=readme)**: use **OpenUPM CLI** or a **scoped registry** plus Package Manager.

### OpenUPM CLI

After installing [openupm-cli](https://openupm.com/docs/getting-started-cli.html), from your project root:

```bash
openupm add jp.nobnak.gpu_uniform_grid
```

### Unity Package Manager (scoped registry)

1. Open **Edit → Project Settings → Package Manager**.
2. Under **Scoped Registries**, add:
   - **URL:** `https://package.openupm.com`
   - **Scope(s):** `jp.nobnak`
3. Open **Window → Package Manager**, switch the registry dropdown to **My Registries** (or your OpenUPM entry).
4. Find **GPU Uniform Grid** and click **Install**.

Or add to `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [ "jp.nobnak" ]
    }
  ],
  "dependencies": {
    "jp.nobnak.gpu_uniform_grid": "1.7.1"
  }
}
```

Pin the version to the current release on [OpenUPM](https://openupm.com/packages/jp.nobnak.gpu_uniform_grid/).

---

## Usage (overview)

1. Build **`UniformGridParams`** (e.g. `gridCenter`, `gridSize`, `bitsPerAxis`, `elementCapacity`).
2. **`new GPUUniformGrid(gridParams)`** — call **`Dispose()`** when done.
3. Each frame you rebuild the grid: **`Reset()`** clears `cellHead` / `cellNext`.
4. Before your insert pass: **`grid.SetParams(compute, kernel)`** or **`SetParamsGlobal()`** so shaders see buffers and grid constants.
5. In HLSL, call **`InsertElementIdAtPosition`** or **`UniformGrid_InsertElementIDAtCellID`** to insert element IDs.

**Example (this repository):** **[`Assets/Samples`](Assets/Samples)** — [`ParticleDataUploader.cs`](Assets/Samples/ParticleDataUploader.cs) dispatches a compute kernel that inserts into the grid. CPU-upload proximity demos live under **[`Assets/Samples/UniformGrid`](Assets/Samples/UniformGrid)** (`Proximity3D`, `Proximity2D`, `Scenes`). (Clone the repo; the UPM package on OpenUPM is the library under `Packages/jp.nobnak.gpu_uniform_grid`.)

---

## References

[^1]: Davide Barbieri, Valeria Cardellini, and Salvatore Filippone. 2013. *Fast Uniform Grid Construction on GPGPUs Using Atomic Operations.* International Conference on Parallel Computing (2013). https://doi.org/10.3233/978-1-61499-381-0-295
