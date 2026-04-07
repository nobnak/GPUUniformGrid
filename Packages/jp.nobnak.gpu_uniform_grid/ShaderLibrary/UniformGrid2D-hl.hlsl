#ifndef __UNIFORM_GRID_2D_HL_HLSL__
#define __UNIFORM_GRID_2D_HL_HLSL__

#include "UniformGrid2D.hlsl"

void InsertElementIdAtPosition2D(float2 position, uint elementId) {
	float2 cellPosition = UniformGrid2D_GetCellPosition(position);
	if (!UniformGrid2D_IsCellPositionValid(cellPosition))
		return;
	uint cellID = MortonCode_Encode2(uint2(cellPosition));
	UniformGrid2D_InsertElementIDAtCellID(cellID, elementId);
}

#endif
