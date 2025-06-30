Shader "NanoverIMD/Opaque/Hyperballs"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Diffuse ("Diffuse", Range(0, 1)) = 0.5
        _ParticleScale ("Particle Scale", Float) = 1
        _EdgeSharpness ("Edge Sharpness", Range(0, 1)) = 0
        _Tension ("Tension", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
        }
        LOD 100
        
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
           
            #pragma instancing_options procedural:setup
            
            #define POSITION_ARRAY
            #define EDGE_ARRAY
            #pragma multi_compile __ SCALE_ARRAY
            #pragma multi_compile __ COLOR_ARRAY
            #pragma multi_compile __ FILTER_ARRAY
            
            // Is the edge smooth (0) or sharp (1)
            float _EdgeSharpness;
            
            // The scales to apply to the particles
            float _ParticleScale;
            
            // Color multiplier
            float4 _Color;
            
            // Diffuse factor (0 for flat, 1 for full diffuse)
            float _Diffuse;
            
            // Tension of the hyperballs
            float _Tension;
            
            #include "UnityCG.cginc"
            #include "../Instancing.cginc"
            #include "../Transformation.cginc"
            #include "../Intersection.cginc"
            #include "../Depth.cginc"
            #include "../Lighting.cginc"
            
            void setup() {
                float3 edgeStartPoint = edge_position(0);
                float3 edgeEndPoint = edge_position(1);
                
                // Transformation of box
                float scale = max(edge_scale(0), edge_scale(1)) * _ParticleScale;
                setup_isotropic_edge_transformation(edgeStartPoint, edgeEndPoint, scale);
            }
            
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDirection : TEXCOORD1;
                float3 bondStart : TEXCOORD2;
                float3 bondDir : TEXCOORD3;
                float4 bondConst : TEXCOORD4;
                float4 color1 : TEXCOORD5;
                float4 color2 : TEXCOORD6;
            };
            
            v2f vert (appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                
                o.vertex = UnityObjectToClipPos(i.vertex);
                
                float3 v = mul(unity_ObjectToWorld, i.vertex);
                float3 c = _WorldSpaceCameraPos.xyz;
                
                o.rayOrigin = v;
                o.rayDirection = v - c;
                
                float3 p1 = mul(ObjectToWorld, float4(edge_position(0).xyz, 1));
                float3 p2 = mul(ObjectToWorld, float4(edge_position(1).xyz, 1));
                
                o.bondStart = p1;
                o.bondDir = normalize(p2 - p1);
                
                float global_scale = length(ObjectToWorld._11_21_31);
                
                float dist = length(p2 - p1);
              
                float R1 = global_scale * edge_scale(0) * _ParticleScale * 0.5;
                R1 = R1 * R1;
                
                float R2 = global_scale * edge_scale(1) * _ParticleScale * 0.5;
                R2 = R2 * R2;
                
                float gamma = _Tension;
                float G = 1 + gamma * gamma;
                float U = (R1 - R2) / (2.0 * dist) + (dist  * (G - 1)) / (2.0 * G);
               
                
                o.bondConst.x = R1;
                o.bondConst.y = G;
                o.bondConst.z = U;
                o.bondConst.w = U + dist / G;
                
                o.color1 = _Color * edge_color(0);
                o.color2 = _Color * edge_color(1);
                
                return o;
            }
            
            float quad(float4 l, float4x4 m, float4 r) {
                return dot(l, mul(m, r));
            }
            
            float quad(float4 l, float4x4 m) {
                return dot(l, mul(m, l));
            }
            
            struct fout {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };
            
            float4x4 quadratic_form(v2f i) {
                float4x4 mat = 0;
                
                float3 p = i.bondStart;
                float3 d = i.bondDir;
                float R1 = i.bondConst.x;
                float G = i.bondConst.y;
                float U = i.bondConst.z;
                
                float pdU = dot(p, d) + U;
                
                mat._11_12_13_14 = float4(1 - d.x * d.x * G, -d.x * d.y * G, -d.x * d.z * G, d.x * pdU * G - p.x);
                mat._21_22_23_24 = float4(-d.y * d.x * G, 1 - d.y * d.y * G, -d.y * d.z * G, d.y * pdU * G - p.y);
                mat._31_32_33_34 = float4(-d.z * d.x * G, -d.z * d.y * G, 1 - d.z * d.z * G, d.z * pdU * G - p.z);
                mat._41_42_43_44 = float4(d.x * pdU * G - p.x, d.y * pdU * G - p.y, d.z * pdU * G - p.z, dot(p, p) - R1 - G * pdU * pdU);
               
                return mat;
            }
            
            fout frag (v2f i)
            {
                fout o;
                o.color = float4(1,1,1,1);
                
                float4 p = float4(i.rayOrigin, 1);
                float4 d = float4(i.rayDirection, 0);
                
                float4x4 quad_form = quadratic_form(i);
                
                float dd = quad(d, quad_form);
                float pd = quad(p, quad_form, d);
                float pp = quad(p, quad_form);
               
                float b2a = pd / dd;
                float ca = pp / dd;
                
                float disc = b2a * b2a - ca;
                
                if(disc < 0)
                    discard;
                    
                float t = -b2a - sqrt(disc);
                
                float3 r = p + d * t;
                float3 n = float3(dot(quad_form._11_12_13_14, float4(r, 1)), dot(quad_form._21_22_23_24, float4(r, 1)), dot(quad_form._31_32_33_34, float4(r, 1)));
                 
                if(dot(n, d) > 0) {
                    t = -b2a + sqrt(disc);
                    r = p + d * t;
                    n = float3(dot(quad_form._11_12_13_14, float4(r, 1)), dot(quad_form._21_22_23_24, float4(r, 1)), dot(quad_form._31_32_33_34, float4(r, 1)));
                }
               
                
                
                float3 bp = i.bondStart;
                float3 bd = i.bondDir;
                
                float sgned = dot(r - bp, bd);
                
                float G = i.bondConst.y;
                float U = i.bondConst.z;
                float U2 = i.bondConst.w;
                
                if(sgned < U || sgned > U2)
                    discard;
                    
                float bias = (sgned - U) / (U2 - U);
                
               
                n = normalize(n);
                
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                
                float cutoff = U / ((G-1) * (U2 - U));
               
                
                float power = log(0.5) / log(cutoff);
                bias = clamp((lerp(pow(bias, log(0.5) / log(cutoff)), 1 - pow(1 - bias, log(0.5) / log(1-cutoff)), step(cutoff, 0.5)) - 0.5) / ((1-_EdgeSharpness) + 0.0001) + 0.5, 0, 1);
    
                o.color = DIFFUSE(lerp(i.color1, i.color2, bias), n, l, _Diffuse);
                
                OUTPUT_FRAG_DEPTH(o, p + d * t);
                return o;
            }
            
            ENDCG
        }
    }
}