using System;
using Slash.Unity.DataBind.Core.Data;
using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts.Features.Market
{
    public class UserItemContext: Context
    {
        #region ItemName Property

        private readonly Property<string> itemNameProperty = new Property<string>();

        public string ItemName
        {
            get { return itemNameProperty.Value; }
            set { itemNameProperty.Value = value; }
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

        #region IsOnSale Property

        private readonly Property<bool> isOnSaleProperty = new Property<bool>();

        public bool IsOnSale
        {
            get { return isOnSaleProperty.Value; }
            set { isOnSaleProperty.Value = value; }
        }

        #endregion

        


        public string ItemId;
        
        public Action OnSell;
        public Action OnUnlist;

        public void SellCommand()
        {
            OnSell?.Invoke();
        }

        public void UnlistCommand()
        {
            OnUnlist?.Invoke();
        }

    }
}