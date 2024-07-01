#ifdef _MOMENT_SINGLE_PRECISION
TEXTURE2D_X_FLOAT(_B0);
TEXTURE2D_X_FLOAT(_B1);

#if defined(_MOMENT8) || defined(_MOMENT6)
TEXTURE2D_X_FLOAT(_B2);
#endif
#else
TEXTURE2D_X_HALF(_B0);
TEXTURE2D_X_HALF(_B1);

#if defined(_MOMENT8) || defined(_MOMENT6)
TEXTURE2D_X_HALF(_B2);
#endif
#endif

float4 _B0_TexelSize;
float _MOIT_MomentBias;
//float _MomentBias; // switched to global
float _Overestimation = 0.25f; // overestimation can be declared properties in the shader, but the 0.25f default is the recommended value in the paper
float4 _WrappingZoneParameters;

// sampler_PointClamp sampler_LinearClamp
#define singleSampler sampler_LinearClamp
#define sampler_B0 singleSampler
#define sampler_B1 singleSampler
#if defined(_MOMENT8) || defined(_MOMENT6)
	#define sampler_B2 singleSampler
#endif

// TODO: check if can use LOAD_FRAMEBUFFER_X_INPUT(index, positionCS.xy); (float2 p = positionCS.xy)

void ResolveMoments(out float td, out float tt, float vd, float2 p)
{
	float d = WarpDepth(vd);
	td = 1;
	tt = 1;
	float b0 = SAMPLE_TEXTURE2D_X(_B0, sampler_B0, p).r;

	// Return early if the surface is fully transparent
	clip(b0 - 0.00100050033f);

	tt = exp(-b0);

	float4 b1 = SAMPLE_TEXTURE2D_X(_B1, sampler_B1, p);
	//b1 /= b0; // had to move as quantized does things different
#ifdef _MOMENT8
	float4 b2 = SAMPLE_TEXTURE2D_X(_B2, sampler_B2, p);
	//b2 /= b0;
#ifdef _TRIGONOMETRIC
	b1 /= b0;
	b2 /= b0;
	float2 tb[4];
	tb[0] = b1.xy;
	tb[1] = b1.zw;
	tb[2] = b2.xy;
	tb[3] = b2.zw;
	td = ComputeTransmittanceTrigonometric(b0, tb, d, _MOIT_MomentBias, _Overestimation, _WrappingZoneParameters);
#else // 8 POWER MOMENTS
#if _MOMENT_SINGLE_PRECISION
	b1 /= b0;
	b2 /= b0;
	float4 be = float4(b1.yw, b2.yw);
	float4 bo = float4(b1.xz, b2.xz);

	const float bias[8] = { 0, 0.75, 0, 0.67666666666666664, 0, 0.64, 0, 0.60030303030303034 };
	//td = ComputeTransmittance(b0, be, bo, d, _MOIT_MomentBias, _Overestimation, bias);
#else // HALF (DEQUANTIZE)
	float4 beq = float4(b1.yw, b2.yw);
	float4 boq = float4(b1.xz, b2.xz);
	float4 be, bo;
	offsetAndDequantizeMoments(be, bo, beq, boq);
	const float bias[8] = { 0, 0.42474916387959866, 0, 0.22407802675585284, 0, 0.15369230769230768, 0, 0.12900440529089119 };
#endif
	td = ComputeTransmittance(b0, be, bo, d, _MOIT_MomentBias, _Overestimation, bias);
#endif
#elif defined(_MOMENT6)
	float2 b2 = SAMPLE_TEXTURE2D_X(_B2, sampler_B2, p).rg;
	//b2 /= b0;
#ifdef _TRIGONOMETRIC
	b1 /= b0;
	b2 /= b0;
	float2 tb[3];
	tb[0] = b1.xy;
	tb[1] = b1.zw;
	tb[2] = b2.xy;
	td = ComputeTransmittanceTrigonometric(b0, tb, d, _MOIT_MomentBias, _Overestimation, _WrappingZoneParameters);
#else // 6 POWER MOMENTS
#if _MOMENT_SINGLE_PRECISION
	b1 /= b0;
	b2 /= b0;
	float3 be = float3(b1.yw, b2.y);
	float3 bo = float3(b1.xz, b2.x);

	const float bias[6] = { 0, 0.48, 0, 0.451, 0, 0.45 };
	//td = ComputeTransmittance(b0, be, bo, d, _MOIT_MomentBias, _Overestimation, bias);
#else // DEQUANTIZE
	float3 beq = float3(b1.yw, b2.y);
	float3 boq = float3(b1.xz, b2.x);
	float3 be, bo;
	offsetAndDequantizeMoments(be, bo, beq, boq);
	const float bias[6] = { 0, 0.5566, 0, 0.489, 0, 0.47869382 };
#endif
	td = ComputeTransmittance(b0, be, bo, d, _MOIT_MomentBias, _Overestimation, bias);
#endif
#else // _MOMENT4
#ifdef _TRIGONOMETRIC
	b1 /= b0;
	float2 tb[2];
	tb[0] = b1.xy;
	tb[1] = b1.zw;
	td = ComputeTransmittanceTrigonometric(b0, tb, d, _MOIT_MomentBias, _Overestimation, _WrappingZoneParameters);
#else // 4 POWER MOMENTS
#if _MOMENT_SINGLE_PRECISION
	b1 /= b0;
	float2 be = b1.yw;
	float2 bo = b1.xz;

	const float4 bias = float4 (0, 0.375, 0, 0.375);
	//td = ComputeTransmittance(b0, be, bo, d, _MOIT_MomentBias, _Overestimation, bias);
#else // QUANTIZED
	float2 beq = b1.yw;
	float2 boq = b1.xz;
	float2 be, bo;
	offsetAndDequantizeMoments(be, bo, beq, boq);
	const float4 bias = float4(0, 0.628, 0, 0.628);
#endif
	td = ComputeTransmittance(b0, be, bo, d, _MOIT_MomentBias, _Overestimation, bias);
#endif
#endif
}