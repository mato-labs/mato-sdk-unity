using System;
using System.Linq;
using MatoLabs.mato_sdk.Scripts.Scriptables;
using Newtonsoft.Json;

namespace MatoLabs.mato_sdk.Scripts
{
    public class MarketItem
    {
        [JsonProperty("mint")]
        public string Mint { get; private set; }

        [JsonProperty("price")] public ulong _price;

        [JsonIgnore]
        public double Price
        {
            get
            {
                var token = PublisherSettings.I.Tokens.First(x => x.Mint == PriceMint || x.DevMint == PriceMint);
                var rawPrice = (_price / (float)Math.Pow(10, token.Decimals)) ;
                return rawPrice;
            }
        }

        [JsonProperty("listing_addr")]
        public string ListingAddress { get; private set; }
        
        [JsonProperty("price_mint")]
        public string PriceMint { get; private set; }
    }
}