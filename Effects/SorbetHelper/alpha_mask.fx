// thank you communalhelper (i love stealing code when i have no clue what im doing)
Texture2D normal_texture;
sampler2D normal_sampler = sampler_state
{
    Texture = <normal_texture>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct vertex_output
{
    float4 position : SV_POSITION;
    float4 color    : COLOR0;
    float2 uv       : TEXCOORD0;
};

uniform float4 mask_color = float4(1.0, 1.0, 1.0, 1.0);

float4 pixel_shader(vertex_output input) : COLOR
{
    float4 input_color = tex2D(normal_sampler, input.uv) * input.color;

    return mask_color * input_color.wwww;
}

// unused
float4 pixel_shader_no_transparency(vertex_output input) : COLOR
{
    float4 input_color = tex2D(normal_sampler, input.uv) * input.color;

    if (input_color.w > 0.0)
    {
        return mask_color;
    }
    else
    {
        return mask_color * 0.0;
    }
}

technique alpha_mask
{
    pass apply
    {
        // VertexShader = compile vs_3_0 vertex_shader();
        PixelShader = compile ps_3_0 pixel_shader();
    }
}