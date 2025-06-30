// Shader for drawing a cylinder along the y axis using raycasting. It can use procedural instancing
// to draw multiple cylinders between different points

// The computation for a sphere-ray intersection involves:
//
//   p: the center of the sphere
//   c: the camera position
//   v: the vertex of the bounding shape that has been hit
//   s: the radius of the sphere
//
// The ray is defined as
//
//   d = v - c: the direction of the ray
//   c: the origin of the ray (the camera)
//
// The ray is w = c + d t. 
//
// The intersection occurs when the distance of a ray point from the sphere is equal to the radius s
// It is worth considering the ray distance relative to the sphere center
//
//   q = c - p: the ray origin relative to the sphere center
//   r = w - p = q + d t: the ray relative to the sphere center
//
// The values p, q and s are computed in the vertex shader and passed to the fragment shader.
//
// Here, we must solve the following:
//
//   r.r = s*s
//
// The det(q, d, s) solves the generic quadratic
//
//    (q + d t).(q + d t) = s^2
//
// It returns the first intersection point and the corresponding value t. It discards the pixel if no intersection occurs.

float _Scale;
float4 _Color;
float _Diffuse;

#include "UnityCG.cginc"

#include "../Instancing.cginc"
#include "../Transformation.cginc"
#include "../Intersection.cginc"
#include "../Depth.cginc"
#include "../Lighting.cginc"

void setup() {
    setup_isotropic_transformation(instance_position(), instance_scale() * _Scale);
}

struct appdata
{
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float4 q : TEXCOORD0;
    float4 d : TEXCOORD1;
    float4 color : TEXCOORD2;
};

v2f vert (appdata i)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(i);
    
    o.vertex = UnityObjectToClipPos(i.vertex);
    
    float3 v = mul(unity_ObjectToWorld, i.vertex);
    float3 c = _WorldSpaceCameraPos.xyz;
    
    #if !defined(PROCEDURAL_INSTANCING_ON)
        float3 p = unity_ObjectToWorld._14_24_34;
    #else
        float3 p = mul(ObjectToWorld, float4(instance_position(), 1)).xyz;
    #endif
    float s = length(ObjectToWorld._11_21_31) * 0.5 * instance_scale() * _Scale;
    
    o.q = float4(c - p, s);
    o.d = float4(v - c, 0);
    o.color = _Color * instance_color();
    
    return o;
}

struct fout {
    float4 color : SV_Target;
    float depth : SV_Depth;
};

fout frag (v2f i)
{
    fout o;
    float3 q = i.q.xyz;
    float3 d = i.d.xyz;
    float s = i.q.w;
    
    float4 rt = solve_ray_intersection(q, d, s);
    float3 r = rt.xyz;
    float t = rt.w;
    
    float3 n = normalize(r);
    float3 l = normalize(_WorldSpaceLightPos0.xyz);
    float3 c = _WorldSpaceCameraPos.xyz;
    
    o.color = DIFFUSE(i.color, n, l, _Diffuse);
    OUTPUT_FRAG_DEPTH(o, c + d * t);
    return o;
}