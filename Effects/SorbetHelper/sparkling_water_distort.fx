// simplified version of https://github.com/NoelFB/CelesteEffects/blob/main/Distort.fx, that only does the water displacement.
// small optimisation for sparkling water because when drawing to multiple render targets you can't clear them at the same time with different colors apparently,
//  and vanilla distortion breaks if the background is transparent.

texture base_texture : register(t0);
sampler base_sampler : register(s0);
texture map_texture : register(t1);
sampler map_sampler : register(s1);

struct vertex_output
{
    float4 position : SV_POSITION;
    float4 color    : COLOR0;
    float2 uv       : TEXCOORD0;
};

uniform float water_sine = 0.0;
uniform float water_camera_y = 0.0;
static const float water_alpha = 1.0;

float2 get_displacement(float2 uv)
{
    // normal displacement
    float4 displacement_pixel = tex2D(map_sampler, uv);
    float2 position = uv;
    // position.x += (displacement_pixel.r * 2.0 - 1.0) * 0.044;
    // position.y += (displacement_pixel.g * 2.0 - 1.0) * 0.078;

    // water shifting stuff
    // amount of BLUE describes how FAST it should wave (range 0.0 -> 1.0)
    float shift = water_alpha * sin((uv.y * 180.0 + water_camera_y) * 0.3 - water_sine * displacement_pixel.b) * 0.004;
    position.x += shift * ceil(displacement_pixel.b);

    return position;
}

float4 pixel_shader(vertex_output input) : COLOR0
{
    return tex2D(base_sampler, get_displacement(input.uv));
}

technique sparkling_water_distort
{
    pass apply
    {
        PixelShader = compile ps_3_0 pixel_shader();
    }
};