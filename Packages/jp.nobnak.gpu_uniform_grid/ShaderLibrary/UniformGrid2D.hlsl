#ifndef __UNIFORM_GRID_2D_HLSL__
#define __UNIFORM_GRID_2D_HLSL__

#include "MortonCode2D.hlsl"
#include "UniformGridLinkedList.hlsl"

static const uint UniformGrid2D_InitValue = (uint)-1;

uint UniformGrid2D_cellHead_Len;
uint UniformGrid2D_cellNext_Len;
globallycoherent RWByteAddressBuffer UniformGrid2D_cellHead_rw;
RWByteAddressBuffer UniformGrid2D_cellNext_rw;
ByteAddressBuffer UniformGrid2D_cellHead_r;
ByteAddressBuffer UniformGrid2D_cellNext_r;

float4 UniformGrid2D_cellOffset;
float4 UniformGrid2D_cellSize;
uint UniformGrid2D_cellCount;

bool UniformGrid2D_IsValid() {
	return UniformGrid2D_cellHead_Len > 0 && UniformGrid2D_cellNext_Len > 0;
}

bool UniformGrid2D_IsCellPositionValid(float2 cellPosition) {
	return all(cellPosition >= 0) && all(cellPosition < (int)UniformGrid2D_cellCount);
}

float2 UniformGrid2D_GetCellPosition(float2 position) {
	return (position - UniformGrid2D_cellOffset.xy) / UniformGrid2D_cellSize.xy;
}

uint UniformGrid2D_GetCellID(float2 position) {
	float2 cellPosition = UniformGrid2D_GetCellPosition(position);
	uint2 cellIndex = uint2(cellPosition);
	return MortonCode_Encode2(cellIndex);
}

void UniformGrid2D_InsertElementIDAtCellID(uint cellID, uint elementID) {
	UG_LL_INTERLOCKED_INSERT(UniformGrid2D_cellHead_rw, UniformGrid2D_cellNext_rw, cellID, elementID);
}

uint UniformGrid2D_GetHeadElementID(uint cellID) {
	return UG_LL_LOAD_HEAD(UniformGrid2D_cellHead_r, cellID);
}

uint UniformGrid2D_GetNextElementID(uint elementID) {
	return UG_LL_LOAD_NEXT(UniformGrid2D_cellNext_r, elementID);
}

void UniformGrid2D_SetHeadElementID(uint cellID, uint value) {
	UG_LL_STORE_HEAD(UniformGrid2D_cellHead_rw, cellID, value);
}

void UniformGrid2D_SetNextElementID(uint elementID, uint value) {
	UG_LL_STORE_NEXT(UniformGrid2D_cellNext_rw, elementID, value);
}

void UniformGrid2D_GetCellRange(float2 cellPosition, int2 cellSpan, out int2 cellIndex0, out int2 cellIndex1) {
	cellIndex0 = int2(clamp(cellPosition - cellSpan, 0, UniformGrid2D_cellCount));
	cellIndex1 = int2(clamp(cellPosition + cellSpan + 1, 0, UniformGrid2D_cellCount));
}

// 距離 R の円盤を覆う AABB セル範囲（各軸 ceil(R / cellSize)）
void UniformGrid2D_GetCellRangeForRadius(float2 cellPosition, float radiusWorld, out int2 cellIndex0, out int2 cellIndex1) {
	float2 cs = UniformGrid2D_cellSize.xy;
	int2 span = (int2)ceil(radiusWorld / cs);
	uint n = UniformGrid2D_cellCount;
	span = min(span, int2((int)n, (int)n));
	UniformGrid2D_GetCellRange(cellPosition, span, cellIndex0, cellIndex1);
}

#endif
