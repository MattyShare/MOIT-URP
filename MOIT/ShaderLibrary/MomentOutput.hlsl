#if _MOMENT_HALF_PRECISION
	#define f1 half
	#define f2 half2
	#define f4 half4
#else // _MOMENT_SINGLE_PRECISION
	#define f1 float
	#define f2 float2
	#define f4 float4
#endif

struct MomentOutput
{
	f1 b0 : SV_Target0;
	f4 b1 : SV_Target1;
#ifdef _MOMENT8
	f4 b2 : SV_Target2;
#elif defined(_MOMENT6)
	f2 b2 : SV_Target2;
#endif
};