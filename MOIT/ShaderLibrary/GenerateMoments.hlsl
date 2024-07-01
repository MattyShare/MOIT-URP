#include "MomentOutput.hlsl"

// DONTBLOWUP is NOT part of the original paper, this is a hack i had to do to make this implementation usable

//#define ABSORPTION_EPSILON max(REAL_MIN, 1e-5)
#if _MOMENT_HALF_PRECISION
//#define DONTBLOWUP HALF_MIN
#define DONTBLOWUP HALF_EPS
#else
//#define DONTBLOWUP FLT_MIN
#define DONTBLOWUP FLT_EPS
#endif

float4 _WrappingZoneParameters;
/*! This function implements complex multiplication.*/
float2 Multiply(float2 LHS, float2 RHS) {
	return float2(LHS.x * RHS.x - LHS.y * RHS.y, LHS.x * RHS.y + LHS.y * RHS.x);
}

// if ROV, set to the stored moments from the rasterizer order view and write the new moments back
// no ROVs so only single precision works - needs further changes for quantized to read/write moments
MomentOutput GenerateMoments(float vd, float t)
{
	t = max(t, DONTBLOWUP); // we have a blowup issue when t (1 - alpha) is close to 0, empirically it happens above 0.99 alpha so let's lock it around there (no visual issues, at that point its pretty indiscernable from opaque)
	
	// Return early if the surface is fully transparent
	clip(0.9999999f - t);

	float a = -log(t);
	float d = WarpDepth(vd);

	MomentOutput output;
	output.b0 = a;

#ifdef _TRIGONOMETRIC
	float p = mad(d, _WrappingZoneParameters.y, _WrappingZoneParameters.y);
	float2 c;
	sincos(p, c.y, c.x);
	float2 c2 = Multiply(c, c);
	output.b1 = float4(c, c2) * a;
#ifdef _MOMENT8
	output.b2 = float4(Multiply(c, c2), Multiply(c2, c2)) * a;
#elif defined(_MOMENT6)
	output.b2 = Multiply(c, c2) * a;
#endif
#else // not _TRIGONOMETRIC
	float d2 = d * d;
	float d4 = d2 * d2;

#if _MOMENT_HALF_PRECISION // QUANTIZE (ROVs ONLY)
#ifdef _MOMENT8
	float4 b_even = (float4) 0;
	float4 b_odd = (float4) 0;;
	offsetMoments(b_even, b_odd, -1.0);
	b_even *= output.b0;
	b_odd *= output.b0;

	float d6 = d4 * d2;
	float4 b_even_new = float4(d2, d4, d6, d6 * d2);
	float4 b_odd_new = float4(d, d2 * d, d4 * d, d6 * d);
	float4 b_even_new_q, b_odd_new_q;
#elif defined(_MOMENT6)
	float3 b_even = (float3) 0;
	float3 b_odd = (float3) 0;
	offsetMoments(b_even, b_odd, -1.0);
	b_even *= output.b0;
	b_odd *= output.b0;

	float3 b_even_new = float3(d2, d4, d4 * d2);
	float3 b_odd_new = float3(d, d2 * d, d4 * d);
	float3 b_even_new_q, b_odd_new_q;
#else // _MOMENT4
	float2 b_even = (float2) 0;
	float2 b_odd = (float2) 0;
	offsetMoments(b_even, b_odd, -1.0);
	b_even *= output.b0;
	b_odd *= output.b0;

	float2 b_even_new = float2(d2, d4);
	float2 b_odd_new = float2(d, d2 * d);
	float2 b_even_new_q, b_odd_new_q;
#endif

	quantizeMoments(b_even_new_q, b_odd_new_q, b_even_new, b_odd_new);
	// combine moments
	b_even += b_even_new_q * a;
	b_odd += b_odd_new_q * a;
	// go back to interval [0, 1]
	b_even /= a;
	b_odd /= a;
	offsetMoments(b_even, b_odd, 1.0);
	output.b1 = float4(b_odd.x, b_even.x, b_odd.y, b_even.y);
#ifdef _MOMENT8
	output.b2 = float4(b_odd.z, b_even.z, b_odd.w, b_even.w);
#elif defined(_MOMENT6)
	output.b2 = float2(b_odd.z, b_even.z);
#endif

#else // _MOMENT_SINGLE_PRECISION
	output.b1 = float4(d, d2, d2 * d, d4) * a;
#ifdef _MOMENT8
	output.b2 = output.b1 * d4;
#elif defined(_MOMENT6)
	output.b2 = output.b1.xy * d4;
#endif
#endif // precision end
#endif // TRIGONOMETRIC end

	return output;
}