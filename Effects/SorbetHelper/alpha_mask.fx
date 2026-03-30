#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

DECLARE_TEXTURE(text, 0);

float4 PS_Pixel(float4 inPosition : SV_Position, float4 inColor : COLOR0, float2 uv : TEXCOORD0) : COLOR
{
    float4 texColor = SAMPLE_TEXTURE(text, uv);

    return inColor * texColor.wwww; // apply texture alpha to vertex color
}

technique AlphaMask
{
    pass apply
    {
        PixelShader = compile ps_3_0 PS_Pixel();
    }
}