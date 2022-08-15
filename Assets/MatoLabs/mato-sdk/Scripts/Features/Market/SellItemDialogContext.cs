using System;
using Slash.Unity.DataBind.Core.Data;
using TMPro;
using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts.Features.Market
{
    public class SellItemDialogContext: Context
    {
        #region IsOpened Property

        private readonly Property<bool> isOpenedProperty = new Property<bool>();

        public bool IsOpened
        {
            get { return isOpenedProperty.Value; }
            set { isOpenedProperty.Value = value; }
        }

        #endregion

        #region ItemIcon Property

        private readonly Property<Sprite> itemIconProperty = new Property<Sprite>();

        public Sprite ItemIcon
        {
            get { return itemIconProperty.Value; }
            set { itemIconProperty.Value = value; }
        }

        #endregion

        #region ItemName Property

        private readonly Property<string> itemNameProperty = new Property<string>();

        public string ItemName
        {
            get { return itemNameProperty.Value; }
            set { itemNameProperty.Value = value; }
        }

        #endregion

        #region InputField Property

        private readonly Property<TMP_InputField> inputFieldProperty = new Property<TMP_InputField>();

        public TMP_InputField InputField
        {
            get { return inputFieldProperty.Value; }
            set { inputFieldProperty.Value = value; }
        }

        #endregion



        public Action<float> OnSell;
        
        public void Open(UserItem item)
        {
            InputField.text = string.Empty;
            ItemName = MarketWindowContext.I.ItemResolver.GetNameForId(item.GameUid);
            IsOpened = true;
        }

        public void SellCommand()
        {
            float price;
            if (float.TryParse(InputField.text, out price) && price > 0)
            {
                OnSell?.Invoke(price);
            }
        }


        public void CloseCommand()
        {
            IsOpened = false;
        }
    }
}