Shader "Nanover/Spline/Hermite"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Radius", float) = 1
        _Diffuse ("Diffuse", float) = 0.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            #pragma multi_compile_instancing
            
            #pragma instancing_options procedural:procedural_setup
            
            #include "../Instancing.cginc" 
            #include "Spline.cginc"
            #include "../Transformation.cginc"
            #include "../Lighting.cginc"
            
            float _Radius;
            float4 _Color;
            
            void procedural_setup() {
            }
            
        float4x4 GetHermiteMatrix(float t) {
            SplineSegment curve = instance_spline();
            
            float3 normal = lerp(curve.startNormal, curve.endNormal, t);
            
            float3 tangent = normalize(GetHermiteTangent(t, curve.startPoint, curve.startTangent, curve.endPoint, curve.endTangent));
            
            float3 pos = GetHermitePoint(t, curve.startPoint, curve.startTangent, curve.endPoint, curve.endTangent);
            
            normal = normal - dot(normal, tangent) * tangent;
            
            float3 right = -cross(normal, tangent);
            
            float width = lerp(curve.startScale.x, curve.endScale.x, t);
            float depth = lerp(curve.startScale.y, curve.endScale.y, t);
            
            float scale = _Radius * lerp(curve.startScale.x, curve.endScale.x, t);
            
            float signX = sign(determinant(ObjectToWorld));
            
            float4x4 mat = get_transformation_matrix(signX*scale*normalize(right.xyz), scale*tangent.xyz, scale*normalize(normal.xyz), pos.xyz);
            
            return mat;
        }

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : TEXCOORD0;
                float4 worldVertex : TEXCOORD1;
                float4 normal : TEXCOORD2;
            };
            
            float _Diffuse;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                SplineSegment spline = instance_spline();
                
                float bias = v.vertex.y;
                float4x4 mat = GetHermiteMatrix(bias);
                v.vertex.y = 0;
                  
                v.vertex = mul(mat, float4(v.vertex.xyz, 1));
                o.normal = normalize(mul(mat, float4(v.normal.xyz, 0)));
                
                v.vertex = mul(ObjectToWorld, float4(v.vertex.xyz, 1));
                o.normal = mul(ObjectToWorldInverseTranspose, float4(o.normal.xyz, 0));
                
                float t = smoothstep(0, 1, bias);
            
                o.color = _Color * lerp(pow(spline.startColor, 2.2), pow(spline.endColor, 2.2), t);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.worldVertex = v.vertex;
                
                return o;
            }
            
            #include "Lighting.cginc"
            
            float4 frag (v2f i) : SV_Target
            {
                float4 color = i.color;
                float3 n = normalize(i.normal);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 c = _WorldSpaceCameraPos.xyz;
                
                color = DIFFUSE(color, n, l, _Diffuse);
                
                return color;
            }
            ENDCG
        }
    }
}
