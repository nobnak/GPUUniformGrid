# Single-pass Uniform Grid for Unity 

A single-pass method of Uniform Grid construction on GPU based on the article *Fast Uniform Grid Construction on GPGPUs Using Atomic Operations* [^1]. This asset is released as UPM package[^2].

[![Dense grid visualization](http://img.youtube.com/vi/GRpFk6DCQ8U/mqdefault.jpg)](https://youtube.com/shorts/GRpFk6DCQ8U)<br>
Dense particle highlighting

[![Dense grid visualization](http://img.youtube.com/vi/GsY-AYIolQ8/mqdefault.jpg)](https://youtube.com/shorts/GsY-AYIolQ8)<br>
Dense grid visualization

[![Thumbnail](http://img.youtube.com/vi/NKYRA955oSE/mqdefault.jpg)](https://youtu.be/NKYRA955oSE)<br>
Close look at active cells

[![Thumbnail](http://img.youtube.com/vi/8GmqgaxiQ2g/mqdefault.jpg)](https://youtu.be/8GmqgaxiQ2g)<br>
Variable grid size

## Installation
Add scoped repository:

- URL: https://package.openupm.com
- Scope: jp.nobnak

Install "GPU Uniform Grid" on Package Manager.

## Usage
- Instantiate [GPUUniformGrid.cs](Packages/jp.nobnak.gpu_uniform_grid/Runtime/GPUUniformGrid.cs) with Uniform Grid related parameters.
- On each update
  - Call C# method Reset() method on GPUUniformGrid
  - Insert each element ID into a cell by using shader function [InsertElementIdAtPosition()](https://github.com/nobnak/GPUUniformGrid/blob/8f654265b23329522e13f138438bcf27ac579c98/Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl#L45)
- Uniform grid data structure, that encapsulates all element IDs, was obtained

[^1]: Davide Barbieri, Valeria Cardellini, and Salvatore Filippone. 2013. Fast Uniform Grid Construction on GPGPUs Using Atomic Operations. International Conference on Parallel Computing (2013). https://doi.org/10.3233/978-1-61499-381-0-295
[^2]: https://openupm.com/packages/jp.nobnak.gpu_uniform_grid/
