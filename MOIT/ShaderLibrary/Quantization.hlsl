// we use this to prevent artifacts with half (16bit) moments
// this is only for power moments as trigonometric moments do not suffer as much from rounding errors (quote from the guys who actually did this technique)
// see https://momentsingraphics.de/I3D2018.html

// Definition of utility functions for quantization and dequantization of power moments stored in 16 bits per moment

// NOTE : It seems that ROVs are necessary for this to work, unfortunately this is out of scope for now

// 4 MOMENTS
void offsetMoments(inout float2 b_even, inout float2 b_odd, float sign)
{
	b_odd += 0.5 * sign;
}

void quantizeMoments(out float2 b_even_q, out float2 b_odd_q, float2 b_even, float2 b_odd)
{
	b_odd_q = mul(b_odd, float2x2(1.5f, sqrt(3.0f) * 0.5f, -2.0f, -sqrt(3.0f) * 2.0f / 9.0f));
	b_even_q = mul(b_even, float2x2(4.0f, 0.5f, -4.0f, 0.5f));
}

void offsetAndDequantizeMoments(out float2 b_even, out float2 b_odd, float2 b_even_q, float2 b_odd_q)
{
	offsetMoments(b_even_q, b_odd_q, -1.0);
	b_odd = mul(b_odd_q, float2x2(-1.0f / 3.0f, -0.75f, sqrt(3.0f), 0.75f * sqrt(3.0f)));
	b_even = mul(b_even_q, float2x2(0.125f, -0.125f, 1.0f, 1.0f));
}

// 6 MOMENTS
void offsetMoments(inout float3 b_even, inout float3 b_odd, float sign)
{
	b_odd += 0.5 * sign;
	b_even.z += 0.018888946f * sign;
}

void quantizeMoments(out float3 b_even_q, out float3 b_odd_q, float3 b_even, float3 b_odd)
{
	const float3x3 QuantizationMatrixOdd = float3x3(
		2.5f, -1.87499864450f, 1.26583039016f,
		-10.0f, 4.20757543111f, -1.47644882902f,
		8.0f, -1.83257678661f, 0.71061660238f);
	const float3x3 QuantizationMatrixEven = float3x3(
		4.0f, 9.0f, -0.57759806484f,
		-4.0f, -24.0f, 4.61936647543f,
		0.0f, 16.0f, -3.07953906655f);
	b_odd_q = mul(b_odd, QuantizationMatrixOdd);
	b_even_q = mul(b_even, QuantizationMatrixEven);
}

void offsetAndDequantizeMoments(out float3 b_even, out float3 b_odd, float3 b_even_q, float3 b_odd_q)
{
	const float3x3 QuantizationMatrixOdd = float3x3(
		-0.02877789192f, 0.09995235706f, 0.25893353755f,
		0.47635550422f, 0.84532580931f, 0.90779616657f,
		1.55242808973f, 1.05472570761f, 0.83327335647f);
	const float3x3 QuantizationMatrixEven = float3x3(
		0.00001253044f, -0.24998746956f, -0.37498825271f,
		0.16668494186f, 0.16668494186f, 0.21876713299f,
		0.86602540579f, 0.86602540579f, 0.81189881793f);
	offsetMoments(b_even_q, b_odd_q, -1.0);
	b_odd = mul(b_odd_q, QuantizationMatrixOdd);
	b_even = mul(b_even_q, QuantizationMatrixEven);
}

// 8 MOMENTS
void offsetMoments(inout float4 b_even, inout float4 b_odd, float sign)
{
	b_odd += 0.5 * sign;
	b_even += float4(0.972481993925964, 1.0, 0.999179192513328, 0.991778293073131) * sign;
}

void quantizeMoments(out float4 b_even_q, out float4 b_odd_q, float4 b_even, float4 b_odd)
{
	const float4x4 mat_odd = float4x4(3.48044635732474, -27.5760737514826, 55.1267384344761, -31.5311110403183,
		1.26797185782836, -0.928755808743913, -2.07520453231032, 1.23598848322588,
		-2.1671560004294, 6.17950199592966, -0.276515571579297, -4.23583042392097,
		0.974332879165755, -0.443426830933027, -0.360491648368785, 0.310149466050223);
	const float4x4 mat_even = float4x4(0.280504133158527, -0.757633844606942, 0.392179589334688, -0.887531871812237,
		-2.01362265883247, 0.221551373038988, -1.06107954265125, 2.83887201588367,
		-7.31010494985321, 13.9855979699139, -0.114305766176437, -7.4361899359832,
		-15.8954215629556, 79.6186327084103, -127.457278992502, 63.7349456687829);
	b_odd_q = mul(mat_odd, b_odd);
	b_even_q = mul(mat_even, b_even);
}

void offsetAndDequantizeMoments(out float4 b_even, out float4 b_odd, float4 b_even_q, float4 b_odd_q)
{
	const float4x4 mat_odd = float4x4(-0.00482399708502382, -0.423201508674231, 0.0348312382605129, 1.67179208266592,
		-0.0233402218644408, -0.832829097046478, 0.0193406040499625, 1.21021509068975,
		-0.010888537031885, -0.926393772997063, -0.11723394414779, 0.983723301818275,
		-0.0308713357806732, -0.937989172670245, -0.218033377677099, 0.845991731322996);
	const float4x4 mat_even = float4x4(-0.976220278891035, -0.456139260269401, -0.0504335521016742, 0.000838800390651085,
		-1.04828341778299, -0.229726640510149, 0.0259608334616091, -0.00133632693205861,
		-1.03115268628604, -0.077844420809897, 0.00443408851014257, -0.0103744938457406,
		-0.996038443434636, 0.0175438624416783, -0.0361414253243963, -0.00317839994022725);
	offsetMoments(b_even_q, b_odd_q, -1.0);
	b_odd = mul(mat_odd, b_odd_q);
	b_even = mul(mat_even, b_even_q);
}