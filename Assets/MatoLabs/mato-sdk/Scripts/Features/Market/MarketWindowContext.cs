using System;
using System.Collections.Generic;
using System.Linq;
using MatoLabs.mato_sdk.Scripts.Scriptables;
using Slash.Unity.DataBind.Core.Data;
using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts.Features.Market
{
    public class MarketWindowContext: Context
    {
        #region IsOpened Property

        private readonly Property<bool> isOpenedProperty = new Property<bool>();

        public bool IsOpened
        {
            get { return isOpenedProperty.Value; }
            set { isOpenedProperty.Value = value; }
        }

        #endregion

        #region IsMarketTab Property

        private readonly Property<bool> isMarketTabProperty = new Property<bool>();

        public bool IsMarketTab
        {
            get { return isMarketTabProperty.Value; }
            set { isMarketTabProperty.Value = value; }
        }

        #endregion

        #region IsMyItemsTab Property

        private readonly Property<bool> isMyItemsTabProperty = new Property<bool>();

        public bool IsMyItemsTab
        {
            get { return isMyItemsTabProperty.Value; }
            set { isMyItemsTabProperty.Value = value; }
        }

        #endregion

        #region Title Property

        private readonly Property<string> titleProperty = new Property<string>();

        public string Title
        {
            get { return titleProperty.Value; }
            set { titleProperty.Value = value; }
        }

        #endregion


        #region MarketItems Property

        private readonly Property<Collection<MarketItemContext>> marketItemsProperty = new(new Collection<MarketItemContext>());

        public Collection<MarketItemContext> MarketItems
        {
            get { return marketItemsProperty.Value; }
            set { marketItemsProperty.Value = value; }
        }

        #endregion

        #region UserItems Property

        private readonly Property<Collection<UserItemContext>> userItemsProperty = new (new Collection<UserItemContext>());

        public Collection<UserItemContext> UserItems
        {
            get { return userItemsProperty.Value; }
            set { userItemsProperty.Value = value; }
        }

        #endregion


        #region SellDialogContext Property

        private readonly Property<SellItemDialogContext> sellDialogContextProperty = new (new SellItemDialogContext());

        public SellItemDialogContext SellDialogContext
        {
            get { return sellDialogContextProperty.Value; }
            set { sellDialogContextProperty.Value = value; }
        }

        #endregion


        public IItemResolver ItemResolver { get; set; }
        
        public static MarketWindowContext I { get; private set; }

        public static Action<string> OnItemSold = (itemId) => { };
        
        public MarketWindowContext()
        {
            I = this;
            ItemResolver = new DefaultItemResolver();
        }

        private void OnSetUserItems(IReadOnlyList<UserItem> items)
        {
            UserItems.Clear();
            
            foreach (var userItem in items)
            {
                for (int i = 0; i < userItem.Count; i++)
                {
                    var itemView = new UserItemContext()
                    {
                        ItemName = ItemResolver.GetNameForId(userItem.GameUid),
                        ItemId = userItem.GameUid,
                        IsOnSale = userItem.IsOnSale,
                        Icon = ItemResolver.GetSpriteForId(userItem.GameUid)
                    };

                    itemView.OnSell = () =>
                    {
                        SellDialogContext.OnSell = (price) =>
                        {
                            MatoSdk.ListItem(userItem.GameUid, price, () =>
                            {
                                SellDialogContext.CloseCommand();
                                OnItemSold?.Invoke(userItem.GameUid);
                                OpenMyItemsTab();
                                
                                Debug.Log("Item listed!");
                            }, () =>
                            {
                                SellDialogContext.CloseCommand();
                                OpenMyItemsTab();
                                
                                Debug.LogError("Failed to list item:(");
                            });
                        };

                        SellDialogContext.ItemIcon = itemView.Icon;
                        SellDialogContext.Open(userItem);
                    };
                
                    itemView.OnUnlist = () =>
                    {
                        itemView.IsOnSale = false;
                    };
                
                    UserItems.Add(itemView);
                }
            }
        }
        
        public static decimal RoundUp(decimal input, int places)
        {
            decimal multiplier = (decimal)Math.Pow(10, places);
            return decimal.Ceiling(input * multiplier) / multiplier;
        }

        private void OnSetMarketItems(IReadOnlyList<MarketItem> items)
        {
            MarketItems.Clear();
            
            foreach (var marketItem in items)
            {
                var itemInfo = PublisherSettings.I.limited_items.FirstOrDefault(x => x.Mint == marketItem.Mint);
                if (itemInfo == null)
                {
                    Debug.LogError($"Failed to find item with mint {marketItem.Mint}");
                    continue;
                }

                var roundedPrice = RoundUp((decimal)marketItem.Price, 4);
                var token = PublisherSettings.I.Tokens.First(x => x.Mint == itemInfo.PriceMint || x.DevMint == itemInfo.PriceMint);
                var itemView = new MarketItemContext()
                {
                    ItemName = ItemResolver.GetNameForId(itemInfo.GameUid),
                    Price = roundedPrice.ToString(),
                    TokenSymbol = token.Symbol,
                    ItemId = itemInfo.GameUid,
                    Icon = ItemResolver.GetSpriteForId(itemInfo.GameUid),
                    ListingAddress = marketItem.ListingAddress
                };

                itemView.OnBuyAction = () =>
                {
                    MatoSdk.BuyMarketItem(itemInfo.GameUid, marketItem.ListingAddress,  () =>
                    {
                        OpenMarketTab();
                        
                        Debug.Log("Item bought!");
                    }, () =>
                    {
                        OpenMarketTab();
                        
                        Debug.LogError("Failed to buy item:(");
                    });
                };
                
                MarketItems.Add(itemView);
            }
        }

        public void OpenCommand()
        {
            OpenMarketTab();
            IsOpened = true;
        }
        
        public void CloseCommand()
        {
            IsOpened = false;

            MarketItems.Clear();
            UserItems.Clear();
        }

        public void OpenMarketTab()
        {
            Title = "Market";
            IsMarketTab = true;
            IsMyItemsTab = false;
            
            MatoSdk.FetchMarketItems((items) =>
            {
                OnSetMarketItems(items);
            }, () =>
            {
                Debug.LogError("Failed to fetch market items");
            });
        }

        public void OpenMyItemsTab()
        {
            Title = "My items";
            IsMarketTab = false;
            IsMyItemsTab = true;

            MatoSdk.FetchUserItems((items) =>
            {
                OnSetUserItems(items);
            }, () =>
            {
                Debug.Log("Failed to fetch user items");
            });
        }


    }
}