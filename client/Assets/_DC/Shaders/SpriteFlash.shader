// Sprite do Dungeon Courier com flash branco e contorno opcional (raia F, 2026-10-09). Built-in RP; GLES3, Vulkan, DX11.
// Base = Sprites/Default (UnitySprites.cginc: cor do SpriteRenderer, flipX por _Flip, alfa pre-multiplicado), mais:
//   _Flash 0..1   mistura a cor da textura para BRANCO antes da tinta do SpriteRenderer (dano: flash 1 com cor branca; flash 1 com cor
//                 limao daria a silhueta limao).
//   _Outline      largura do contorno em texels da folha (0 = desligado: ramo uniforme, os 8 taps nao rodam). So para folha de 1 celula
//                 (cargas): em folha com varias celulas o tap vizinho leria a celula ao lado.
//   _OutlineColor cor e alfa do contorno, desenhado por baixo do sprite (so onde ele e transparente).
// Valores por renderer via MaterialPropertyBlock (Art.Fx). Entra no build pelo material Resources/Materials/SpriteFlash.mat, que o
// referencia (shader usado so por codigo some do APK: licao ARKANA). Sem suporte no aparelho: Fallback Sprites/Default.
Shader "DC/SpriteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Flash ("Flash", Range(0, 1)) = 0
        _Outline ("Outline (texels)", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1, 0.882, 0.302, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnitySprites.cginc"

            float _Flash, _Outline;
            fixed4 _OutlineColor;
            float4 _MainTex_TexelSize;

            // tex2Dlod (sem derivada): seguro dentro do ramo
            fixed Tap(float2 uv) { return tex2Dlod(_MainTex, float4(uv, 0, 0)).a; }

            fixed4 Frag(v2f IN) : SV_Target
            {
                fixed4 t = SampleSpriteTexture(IN.texcoord);
                fixed a = t.a * IN.color.a;
                fixed4 c = fixed4(lerp(t.rgb, fixed3(1, 1, 1), _Flash) * IN.color.rgb * a, a);
                UNITY_BRANCH
                if (_Outline > 0)
                {
                    float2 uv = IN.texcoord, s = _MainTex_TexelSize.xy * _Outline, k = s * 0.7071;
                    fixed n = max(max(max(Tap(uv + float2(s.x, 0)), Tap(uv - float2(s.x, 0))),
                                      max(Tap(uv + float2(0, s.y)), Tap(uv - float2(0, s.y)))),
                                  max(max(Tap(uv + k), Tap(uv - k)),
                                      max(Tap(uv + float2(k.x, -k.y)), Tap(uv + float2(-k.x, k.y)))));
                    fixed o = n * _OutlineColor.a * (1 - a);
                    c += fixed4(_OutlineColor.rgb * o, o);
                }
                return c;
            }
        ENDCG
        }
    }

    Fallback "Sprites/Default"
}
