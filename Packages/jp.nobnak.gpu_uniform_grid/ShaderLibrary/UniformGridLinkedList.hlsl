#ifndef __UNIFORM_GRID_LINKED_LIST_HLSL__
#define __UNIFORM_GRID_LINKED_LIST_HLSL__

// 3D / 2D 共通: Raw バッファ上のセル連結リスト操作（ByteAddressBuffer API）

#define UG_LL_INTERLOCKED_INSERT(headRw, nextRw, cellID, elementID) \
    { uint ug_ll_prev_; \
    (headRw).InterlockedExchange(4u * (cellID), (elementID), ug_ll_prev_); \
    (nextRw).Store(4u * (elementID), ug_ll_prev_); }

#define UG_LL_LOAD_HEAD(headR, cellID) (headR).Load(4u * (cellID))
#define UG_LL_LOAD_NEXT(nextR, elemID) (nextR).Load(4u * (elemID))
#define UG_LL_STORE_HEAD(headRw, cellID, value) (headRw).Store(4u * (cellID), (value))
#define UG_LL_STORE_NEXT(nextRw, elemID, value) (nextRw).Store(4u * (elemID), (value))

#endif
