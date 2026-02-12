Shader "Custom/RoundedTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float roundedBox(float2 uv, float radius)
            {
                float2 center = float2(0.5, 0.5);
                float2 pos = uv - center;
                float2 absPos = abs(pos);
                
                float2 cornerPos = absPos - (0.5 - radius);
                
                if (cornerPos.x > 0 && cornerPos.y > 0)
                {
                    float dist = length(cornerPos);
                    if (dist > radius)
                        return 0;
                }
                
                return 1;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                float alpha = roundedBox(i.uv, _Radius);
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
