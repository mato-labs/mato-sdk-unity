using Newtonsoft.Json;

namespace MatoLabs.mato_sdk.Scripts.Models
{
    public class MarketData
    {
        [JsonProperty("items")]
        public MarketItem[] Items { get; private set; }
    }
}