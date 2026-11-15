using UnityEditor;

namespace Galileo.EditorTools
{
    /// <summary>
    /// New bitmaps dropped into Resources/Galileo/ are imported as uncompressed-size sprites without mipmaps, so they keep
    /// their exact pixel size (the garage parts must line up with each other) instead of being stretched to a power of two.
    /// Only applies the first time a file is imported; settings changed by hand afterwards are kept.
    /// </summary>
    public class GalileoTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Galileo/")) return;
            if (!assetImporter.importSettingsMissing) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
        }
    }
}
