// SPTD: Spherical Pivot Transformed Distributions
// Keep in synch with the c# side (eg in Bind() and for dims)
TEXTURE2D_ARRAY(_PivotData);

#define PIVOT_LUT_SIZE   64
#define PIVOT_LUT_SCALE  ((PIVOT_LUT_SIZE - 1) * rcp(PIVOT_LUT_SIZE))
#define PIVOT_LUT_OFFSET (0.5 * rcp(PIVOT_LUT_SIZE))
