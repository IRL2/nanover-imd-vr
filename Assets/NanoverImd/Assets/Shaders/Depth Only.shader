Shader "Custom/Depth Only"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}
        LOD 200
        ZWrite On
        Blend SrcAlpha One
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
     
            half4 _Color;

            struct v2f {
                float4 pos : SV_POSITION;
            };
     
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos (v.vertex);
                return o;
            }
     
            half4 frag (v2f i) : COLOR
            {
                return _Color;
            }
            ENDCG  
        }
    }
    FallBack "Diffuse"
}
