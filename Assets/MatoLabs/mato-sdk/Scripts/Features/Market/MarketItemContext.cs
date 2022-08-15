using System;
using Slash.Unity.DataBind.Core.Data;
using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts.Features.Market
{
    public class MarketItemContext: Context
    {
        #region ItemName Property

        private readonly Property<string> itemNameProperty = new Property<string>();

        public string ItemName
        {
            get { return itemNameProperty.Value; }
            set { itemNameProperty.Value = value; }
        }

        #endregion

        #region TokenSymbol Property

        private readonly Property<string> tokenSymbolProperty = new Property<string>();

        public string TokenSymbol
        {
            get { return tokenSymbolProperty.Value; }
            set { tokenSymbolProperty.Value = value; }
        }

        #endregion

        #region Price Property

        private readonly Property<string> priceProperty = new Property<string>();

        public string Price
        {
            get { return priceProperty.Value; }
            set { priceProperty.Value = value; }
        }

        #endregion

        #region Icon Property

        private readonly Property<Sprite> iconProperty = new Property<Sprite>();

        public Sprite Icon
        {
            get { return iconProperty.Value; }
            set { iconProperty.Value = value; }
        }

        #endregion


        public Action OnBuyAction;
        public string ItemId;
        public string ListingAddress;
        
        public void BuyCommand()
        {
            OnBuyAction.Invoke();
        }
    }
}