# 固定上限セルグリッド（コンパクト格納）実装指示書

同一パッケージ **`jp.nobnak.gpu_uniform_grid`** に、リンクリスト方式と並んで置く。**セルあたり最大 `_M` 件**の `CellParticles` と、`atomicAdd` による **1 パス構築**。構築・クエリは **粒子軸**のディスパッチと太い `numthreads`（例: 256）。座標は **`UniformGridParams` / `UniformGridParams2D`** と同じ `GridOffset`・`gridCenter`・`gridSize` から `CellSize` を導出する。

---

## フォルダ構成

| パス | 内容 |
|------|------|
| `Runtime/Data/` | 共有パラメータ。コンパクト専用フィールド（分割数・`_M` 等）は必要なら別 struct で追加。 |
| `Runtime/LinkedList/` | `GPUUniformGrid` / `GPUUniformGrid2D`、既存 Provider / Behaviour、リンクリスト関連。 |
| `Runtime/Compact/` | `GPUCompactUniformGrid`（3D）／2D 版、Behaviour、オーバーフロー検知用デバッグ。 |
| `Runtime/Helper/` | 両方式で使うディスパッチ件数計算など（既存を拡張）。 |
| `ShaderLibrary/LinkedList/` | `UniformGrid*.hlsl`、`UniformGridLinkedList.hlsl`、Morton 系。 |
| `ShaderLibrary/Compact/` | `CompactUniformGrid2D.hlsl` 等（線形セル ID、Morton 非依存）。3D 用を並置。 |
| `Resources/Shader/LinkedList/` | `UniformGrid.compute`、`UniformGrid2D.compute`。 |
| `Resources/Shader/Compact/` | `CompactUniformGrid2D.compute` 等。 |

`Resources.Load` 用の **文字列パスは C# 定数と常に一致**させる。

---

## 座標と線形セル ID

**2D:** `GridOffset = gridCenter - gridSize * 0.5`、`CellSize = gridSize / float2(GridWidth, GridHeight)`、`TotalCells = GridWidth * GridHeight`。論理座標 `p`（`float2`）に対し `q = p - GridOffset`、`c = floor(q / CellSize)`。`c` が `[0, GridWidth) × [0, GridHeight)` に入るとき `cell = c.x + c.y * GridWidth`。

**3D（既存立方体グリッドに合わせる）:** `GridOffset = gridCenter - gridSize * 0.5`、スカラー `CellSize = gridSize / Nx`（`Nx == Ny == Nz` 想定）、`TotalCells = Nx * Ny * Nz`。`q = p - GridOffset`、`c = floor(q / CellSize)`。範囲内なら `cell = cx + Nx * (cy + Ny * cz)`。

範囲外の粒子は **構築・クエリでセルへ書かず読まない**。

---

## バッファ

```hlsl
StructuredBuffer<float2> Positions2D;   // 3D は float3 等

RWStructuredBuffer<uint> CellCounts;    // セルへの到着試行回数（_M を超えうる）
RWStructuredBuffer<uint> CellParticles; // TotalCells * _M
```

構築: `idx = atomicAdd(CellCounts[cell], 1)`、`idx < _M` のときだけ `CellParticles[cell * _M + idx] = id`。読み取りの有効個数は **`min(CellCounts[cell], _M)`**。`CellCounts[c] > _M` でそのセルは上限超過（格納漏れ件数は `CellCounts[c] - _M`）。デバッグで検知をオンにしたときは **`Debug.LogWarning`**（セル細分化、`_M` 増加、密度、`gridSize` / `gridCenter` の見直し等）を出す。

---

## cbuffer（例）

```hlsl
cbuffer Params2D {
    uint _NumParticles, _TotalCells, _GridWidth, _GridHeight, _M;
    float2 _GridOffset, _CellSize;
};

cbuffer Params3D {
    uint _NumParticles, _TotalCells, _Nx, _Ny, _Nz, _M;
    float3 _GridOffset;
    float _CellSize;
};
```

---

## セル ID（HLSL）

```hlsl
bool CellID2D(float2 p, out uint cell) {
    float2 q = p - _GridOffset;
    int2 c = (int2)floor(q / _CellSize);
    if (any(c < 0) || c.x >= (int)_GridWidth || c.y >= (int)_GridHeight) { cell = 0; return false; }
    cell = (uint)c.x + (uint)c.y * _GridWidth;
    return true;
}

bool CellID3D(float3 p, out uint cell) {
    float3 q = p - _GridOffset;
    int3 c = (int3)floor(q / _CellSize);
    if (any(c < 0) || c.x >= (int)_Nx || c.y >= (int)_Ny || c.z >= (int)_Nz) { cell = 0; return false; }
    cell = (uint)c.x + (uint)_Nx * ((uint)c.y + (uint)_Ny * (uint)c.z);
    return true;
}
```

---

## 初期化・構築

```hlsl
[numthreads(256,1,1)]
void CS_Init(uint id : SV_DispatchThreadID) {
    if (id < _TotalCells) CellCounts[id] = 0;
}

[numthreads(256,1,1)]
void CS_Build2D(uint id : SV_DispatchThreadID) {
    if (id >= _NumParticles) return;
    uint cell;
    if (!CellID2D(Positions2D[id], cell)) return;
    uint idx = atomicAdd(CellCounts[cell], 1);
    if (idx < _M) CellParticles[cell * _M + idx] = id;
}
```

3D は `CellID3D` と位置バッファに差し替え。クエリは粒子軸で近傍セルを走査し、各セルで **`j < min(CellCounts[ncell], _M)`** として `CellParticles` を読む。
