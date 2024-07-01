#ifndef UNITY_UI_INCLUDED
#define UNITY_UI_INCLUDED

float UnityGet2DClipping(float2 position, float4 clipRect)
{
    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

half4 UnityGetUIDiffuseColor(float2 position, TEXTURE2D_PARAM(mainTexture, sampler_mainTexture), TEXTURE2D_PARAM(alphaTexture, sampler_alphaTexture), half4 textureSampleAdd)
{
    return half4(SAMPLE_TEXTURE2D_X(mainTexture, sampler_mainTexture, position).rgb + textureSampleAdd.rgb, SAMPLE_TEXTURE2D_X(alphaTexture, sampler_alphaTexture, position).r + textureSampleAdd.a);
}

// This piecewise approximation has a precision better than 0.5 / 255 in gamma space over the [0..255] range
// i.e. abs(l2g_exact(g2l_approx(value)) - value) < 0.5 / 255
// It is much more precise than GammaToLinearSpace but remains relatively cheap
half3 UIGammaToLinear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;

    // We should be 0.5 away from any actual gamma value stored in an 8 bit channel
    const half3 split = (half3)0.0725490; // Equals 18.5 / 255
    return (value < split) ? low : high;
}
#endif