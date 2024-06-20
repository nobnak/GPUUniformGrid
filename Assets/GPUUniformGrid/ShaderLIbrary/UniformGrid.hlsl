#ifndef __UNIFORM_GRID_HLSL__
#define __UNIFORM_GRID_HLSL__

#include "MortonCode.hlsl"

static const uint UniformGrid_InitValue = (uint)-1;

uint UniformGrid_cellHead_Len;
uint UniformGrid_cellNext_Len;
globallycoherent RWByteAddressBuffer UniformGrid_cellHead_rw;
RWByteAddressBuffer UniformGrid_cellNext_rw;
ByteAddressBuffer UniformGrid_cellHead_r;
ByteAddressBuffer UniformGrid_cellNext_r;

float4 UniformGrid_cellOffset;
float4 UniformGrid_cellSize;
uint UniformGrid_cellCount;

float3 UniformGrid_GetCellPosition(float3 position) {
	return (position - UniformGrid_cellOffset.xyz) / UniformGrid_cellSize.xyz;
}
uint UniformGrid_GetCellID(float3 position) {
	float3 cellPosition = UniformGrid_GetCellPosition(position);
	uint3 cellIndex = uint3(cellPosition);
	uint cellID = MortonCode_Encode3(cellIndex);
	return cellID;
}
void UniformGrid_InsertElementIDAtCellID(uint cellID, uint elementID) {
	uint lastStartElement;
	UniformGrid_cellHead_rw.InterlockedExchange(4 * cellID, elementID, lastStartElement);
	UniformGrid_cellNext_rw.Store(4 * elementID, lastStartElement);
}

uint UniformGrid_GetHeadElementID(uint cellID) {
	return UniformGrid_cellHead_r.Load(4 * cellID);
}
uint UniformGrid_GetNextElementID(uint elementID) {
	return UniformGrid_cellNext_r.Load(4 * elementID);
}

void UniformGrid_SetHeadElementID (uint cellID, uint value) {
	UniformGrid_cellHead_rw.Store(4 * cellID, value);
}
void UniformGrid_SetNextElementID (uint elementID, uint value) {
	UniformGrid_cellNext_rw.Store(4 * elementID, value);
}
#endif