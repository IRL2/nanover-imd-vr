Shader "Nanover/Offset"
{
    Properties
    {
        _Offset ("Offset", Float) = 0.015
        _Color ("Color", Color) = (1, 1, 1 ,1)
        _Diffuse ("Diffuse", Float) = 0.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "LightMode"="ForwardBase"
        }
        LOD 100
        
        CGINCLUDE
        
        #include "UnityCG.cginc"
        #include "UnityLightingCommon.cginc"
        #include "Lighting.cginc"
        
        struct appdata
        {
            float4 vertex : POSITION;
            float4 normal : NORMAL;
            float4 color : COLOR;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float4 color : TEXCOORD2;
            float4 normal : TEXCOORD0;
        };

        float _Offset;
        float4 _Color;

        v2f offset_vert (appdata v, float mult)
        {
            v2f o;
            
            v.normal *= sign(mult * _Offset) * sign(determinant(unity_ObjectToWorld));
            o.normal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
            
            v.vertex += v.normal * abs(_Offset);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.color = pow(v.color, 2.2);
            return o;
        }

        
        
        ENDCG

        Pass
        {
             Tags {"LightMode"="ForwardBase"}
            Cull Back
            
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float _Diffuse;
            
            v2f vert (appdata v) { return offset_vert(v, 1); }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 color = i.color;
                float3 n = normalize(i.normal.xyz);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                return _Color * DIFFUSE(color, n, l, _Diffuse);
            }

            ENDCG
        }
        
        Pass
        {
             Tags {"LightMode"="ForwardBase"}
            Cull Front
            
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float _Diffuse;
            
            v2f vert (appdata v) { return offset_vert(v, -1); }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 color = i.color;
                float3 n = normalize(i.normal.xyz);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                return _Color * DIFFUSE(color, n, l, _Diffuse);
            }

            ENDCG
        }
        
    }
    CustomEditor "Nanover.Visualisation.Editor.RaytraceEditor"
}
