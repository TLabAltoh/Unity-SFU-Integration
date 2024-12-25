Shader "TLab/SFU/Pointable/Outline"
{
    Properties
    {
        _Color("Color", Color) = (0,1,1,1)
        _Width("Width", Range(0, 0.1)) = .025
        _ZOffset("Z Offset", Range(-0.5, 0.5)) = .2
    }

    SubShader
    {
        GrabPass { }

        Pass
        {
            Name "OUTLINE"

            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            //Blend SrcAlpha OneMinusSrcAlpha
            Blend One OneMinusSrcAlpha
            ZWrite On

            LOD 100

            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float4 _Color;
            uniform float _Width;
            uniform float _ZOffset;
            sampler2D _GrabTexture;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 normal   : TEXCOORD1;
                float4 grabPos  : TEXCOORD2;
            };

            // https://3dcg-school.pro/unity-outline-shader/

            v2f vert(appdata v)
            {
                v2f o;

                float3 positionWS = mul(unity_ObjectToWorld, v.vertex);
                float3 zOffset = normalize(positionWS - _WorldSpaceCameraPos) * _ZOffset;
                o.pos = UnityWorldToClipPos(positionWS + zOffset);

                float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.color.rgb));
                float3 offset = TransformViewToProjection(norm);

                //o.pos.xyz += offset.xyz * UNITY_Z_0_FAR_FROM_CLIPSPACE(o.pos.z) * _Width;
                o.pos.xyz += offset.xyz * _Width;
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.grabPos = ComputeGrabScreenPos(o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 layer0 = tex2Dproj(_GrabTexture, i.grabPos);
                fixed4 effect = _Color * _Color.a;
                layer0.a *= layer0.a;
                layer0.a *= (1. - effect.a);
                layer0.rgb *= layer0.a;
                fixed4 col = effect + layer0;
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}