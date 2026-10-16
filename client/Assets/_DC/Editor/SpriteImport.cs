using System;
using UnityEditor;
using UnityEngine;

namespace DC.EditorTools
{
    /// <summary>
    /// Import das folhas pre-renderizadas (Assets/_DC/Resources/Sprites/**.png, docs/RENDER_SPRITES.md §4): textura crua (Default)
    /// que a SpriteSheet fatia em runtime pelo meta.json, entao nao precisa de Sprite/Multiple nem de isReadable. Sem mipmap
    /// (ortografica 2D), Clamp (celula vizinha nao vaza no Bilinear), teto 4096, sem compressao no Editor; DXT5 (BC3) no Windows;
    /// ASTC 6x6 no Android (sem ele o Unity descomprime para RGBA32: 9x, ART_BIBLE §5). Padrao do SpriteImport do Forge Street.
    /// A arte da interface (Assets/_DC/Resources/UI/**.png: logo, icones, fundo dos menus; client/tools/ui_assets.py) entra igual, mas com
    /// ASTC 4x4 no Android (o 6x6 borra o contorno fino do logo e dos icones).
    /// Mudou alguma regra aqui: suba GetVersion para reimportar.
    /// </summary>
    public sealed class SpriteImport : AssetPostprocessor
    {
        const string Root = "Assets/_DC/Resources/Sprites/", Ui = "Assets/_DC/Resources/UI/";

        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            bool ui = path.StartsWith(Ui, StringComparison.OrdinalIgnoreCase);
            if (!(ui || path.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.isReadable = false;
            ti.maxTextureSize = 4096;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;   // folha de 12 x 224 px nao e potencia de 2: nao reescalar a grade
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            // ponytail: DXT5 e nao BC7 (como no Forge: import rapido); trocar para BC7 se a borda do contorno borrar na foto do PC
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone",
                overridden = true,
                maxTextureSize = 4096,
                format = TextureImporterFormat.DXT5,
            });
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 4096,
                format = ui ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6,
            });
        }
    }
}
