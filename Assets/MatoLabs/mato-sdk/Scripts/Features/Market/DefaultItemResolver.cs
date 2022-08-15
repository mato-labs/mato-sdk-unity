using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts.Features.Market
{
    public class DefaultItemResolver: IItemResolver
    {
        public Sprite GetSpriteForId(string itemId)
        {
            return null;
        }

        public string GetNameForId(string itemId)
        {
            return string.Empty;
        }
    }
}