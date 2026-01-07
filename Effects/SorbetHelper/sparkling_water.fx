texture detail_texture : register(t0);
sampler detail_sampler : register(s0);

float4x4 World;

struct vertex_input
{
    float4 position : POSITION0;
    float4 color    : COLOR0;
};

struct vertex_output
{
    float4 position : SV_POSITION;
    float4 color    : COLOR0;
};

struct pixel_output
{
    float4 color        : COLOR0;
    float4 displacement : COLOR1;
};

static const float texture_size = 128.0;

uniform float4 outline_color;
uniform float4 edge_color;
uniform float4 fill_color;

// caustic scale, caustic alpha, bubble alpha, displacement speed
uniform float4 detail_config = float4(0.8, 0.15, 0.3, 0.25);

uniform float time;
uniform float2 camera_pos;

vertex_output vertex_shader(vertex_input input)
{
    vertex_output output;

    output.position = mul(input.position, World);
    output.color = input.color;

    return output;
}

float clamped_map(float val, float prev_min, float prev_max)
{
    return clamp((val - prev_min) / (prev_max - prev_min), 0.0, 1.0);
}

// noise layer (uses red channel in detail texture)
float noise(float2 position)
{
    position.x = position.x + time * 3.0;
    position.y = position.y + time * 6.0;
    float2 uv = position / texture_size;

    return tex2D(detail_sampler, uv).r;
}

// caustic layer (uses green channel in detail texture)
float caustic(float2 position, float squish)
{
    float texture_squish = squish * 16.0 * clamped_map(abs(squish), 0.25, 0.5);
    position.x = position.x + time * 2.0;
    position.y = position.y - time * 1.0 - texture_squish;
    float2 uv = position / (texture_size * detail_config.x);

    return tex2D(detail_sampler, uv).g;
}

// bubble layer (uses blue channel in detail texture)
float bubble(float2 position)
{
    // position.x = position.x + time * 1.0;
    position.y = position.y + time * 5.0;
    float2 uv = (floor(position) + 0.5) / texture_size;

    return tex2D(detail_sampler, uv).b;
}

pixel_output pixel_shader(vertex_output input)
{
    pixel_output output;

    // red = distance to edge
    // green = caustic squish amount (maps from 0.0/0.5/1.0 to -1.0/0.0/1.0)
    // blue = outline
    float4 mask_color = input.color;
    float center_distance = mask_color.r;
    float caustic_squish = mask_color.g * 2.0 - 1.0;

    // outline
    // probably a better way to do this without branching but im stupid
    if (mask_color.b >= 1.0)
    {
        output.color = outline_color;
        output.displacement = float4(0.5, 0.5, 0.0, 1.0);
        return output;
    }

    float2 world_pos = camera_pos + input.position.xy; // input.uv / pixel;

    // gradient from the edge color to the center color
    float4 base_color = lerp(fill_color, edge_color, center_distance);

    // foam near the edges of the water
    // these thresholds are all largely arbitrary but they seem to look ok i think
    float edge_foam_noise = step(0.45, noise(world_pos));
    float edge_foam = max(step(0.98, center_distance), step(0.93, center_distance) * 0.5 + edge_foam_noise * step(0.82, center_distance) * 0.5);

    // caustics & bubbles
    float caustic_alpha = caustic(world_pos, caustic_squish) * (center_distance * 0.5 + 0.5) * detail_config.y;
    float bubble_alpha = bubble(world_pos) * (center_distance * 0.65 + 0.35) * detail_config.z;

    float detail_alpha = max(edge_foam, bubble_alpha + caustic_alpha);

    output.color = lerp(base_color, outline_color, detail_alpha) * mask_color.a;
    output.displacement = float4(0.5, 0.5, detail_config.w, 1.0);
    return output;
}

technique sparkling_water
{
    pass apply
    {
        VertexShader = compile vs_3_0 vertex_shader();
        PixelShader = compile ps_3_0 pixel_shader();
    }
};