Shader "TLab/SFU/Pointable/Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,1,1,1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = .025
        _ZOffset("Z Offset", Range(-0.5, 0.5)) = .2
    }

    SubShader
    {
        Pass
        {
            Name "OUTLINE"

            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha

            LOD 100

            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

        // Outline
        uniform float _ZOffset;
        uniform float _OutlineWidth;
        uniform float4 _OutlineColor;

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
            float3 normal : NORMAL;
            float4 color  : COLOR;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv  : TEXCOORD0;
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

            //o.pos.xyz += offset.xyz * UNITY_Z_0_FAR_FROM_CLIPSPACE(o.pos.z) * _OutlineWidth;
            o.pos.xyz += offset.xyz * _OutlineWidth;

            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            return _OutlineColor;
        }
        ENDCG
    }
    }
    FallBack "Diffuse"
}