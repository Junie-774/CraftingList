using BitFaster.Caching.Lru;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CraftingList.Utility
{
    internal class IconCache : IDisposable
    {
        private ClassicLru<(uint, bool), TextureWrap> TextureWraps { get; } = new(1000);

        public void Dispose()
        {
            
            foreach (((uint, bool) _, TextureWrap textureWrap) in TextureWraps)
            {
                textureWrap.Dispose();
            }
            
        }

        internal TextureWrap? GetIcon(uint id, bool hq)
        {
            TextureWrap icon1;
            if (this.TextureWraps.TryGet((id, hq), out icon1))
                return icon1;

            TextureWrap? icon2 = hq ? Service.DataManager.GetImGuiTextureHqIcon(id) : Service.DataManager.GetImGuiTextureIcon(id);
            if (icon2 == null)
                return null;

            TextureWraps.AddOrUpdate((id, hq), icon2);
            return icon2;

        }

        internal bool TryGetIcon(uint iconId, bool hq, [MaybeNullWhen(false)] out TextureWrap textureWrap)
        {
            textureWrap = GetIcon(iconId, hq);
            return textureWrap != null;
        }
    }
}
