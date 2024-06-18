#ifndef __UNIFORM_GRID_HLSL__
#define __UNIFORM_GRID_HLSL__

#include "MortonCode.hlsl"

RWByteAddressBuffer UniformGrid_cellHead;
RWByteAddressBuffer UniformGrid_cellNext;
uint UniformGrid_cellHead_Len;
uint UniformGrid_cellNext_Len;

float4 UniformGrid_cellOffset;
float4 UniformGrid_cellSize;
uint UniformGrid_cellCount;

uint UniformGrid_GetCellID(float3 position) {
	float3 cellPosition = (position - UniformGrid_cellOffset.xyz) / UniformGrid_cellSize.xyz;
	uint3 cellIndex = uint3(cellPosition);
	uint cellID = MortonCode_Encode3(cellIndex);
	return cellID;
}
void UniformGrid_InsertElementIDAtCellID(uint cellID, uint elementID) {
	uint lastStartElement;
	UniformGrid_cellHead.InterlockedExchange(cellID * 4, elementID, lastStartElement);
	UniformGrid_cellNext.Store(4 * elementID, lastStartElement);
}

uint UniformGrid_GetHeadElementID(uint cellID) {
	uint headElementID;
	UniformGrid_cellHead.Load(cellID * 4, headElementID);
	return headElementID;
}
uint UniformGrid_GetNextElementID(uint elementID) {
	uint nextElementID;
	UniformGrid_cellNext.Load(4 * elementID, nextElementID);
	return nextElementID;
}

#endif