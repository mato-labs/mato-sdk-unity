using Newtonsoft.Json;

namespace MatoLabs.mato_sdk.Scripts.Models
{
    public class CatalogData
    {
        [JsonProperty("items")]
        public LimitedItem_v2[] Items { get; private set; }
    }
}