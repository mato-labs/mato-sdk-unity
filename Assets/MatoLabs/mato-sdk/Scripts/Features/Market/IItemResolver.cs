using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts.Features.Market
{
    public interface IItemResolver
    {
        Sprite GetSpriteForId(string itemId);
        string GetNameForId(string itemId);

    }
}