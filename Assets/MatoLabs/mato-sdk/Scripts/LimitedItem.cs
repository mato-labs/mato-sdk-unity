using System;
using Newtonsoft.Json;
using Sirenix.Serialization;

namespace MatoLabs.mato_sdk.Scripts
{
    [Serializable]
    public class LimitedItem_v2
    {
        public string ItemName { get; private set; }

        [JsonProperty("uid")] public string GameUid;

        [JsonProperty("supply")] public int Supply;

        [JsonProperty("price")] public int Price;

        [JsonProperty("sold")] public int Sold;
        
        public string tokenSymbol = "USDC";

        [JsonProperty("mint")] public string Mint;

        [JsonProperty("mint_meta")] public string MintMetaAddress;

        [JsonProperty("isActive")] public bool IsEnabled = true;
        
        [JsonProperty("price_mint")] public string PriceMint;
    }
}