using System;
using System.Collections.Generic;
using System.Linq;
using MatoLabs.mato_sdk.Scripts.Models;
using MatoLabs.mato_sdk.Scripts.Scriptables;
using MEC;
using Newtonsoft.Json;
using SolanaWalletAdapter.Scripts;
using UnityEngine;
using UnityEngine.Networking;

namespace MatoLabs.mato_sdk.Scripts
{
    public static class MatoSdk
    {

        static MatoSdk()
        {
        }

        
        public static void BuyItem(string itemId, Action onOk, Action onFailed)
        {
            var mint = PublisherSettings.I.limited_items.First(x => x.GameUid == itemId).Mint;
            var amount = 1;
            
            RunTx(() => $"https://cldfn.com/matosolana/buy/{mint}/{amount}/{PhantomController.Pubkey}", onOk, onFailed);
        }


        public static void ListItem(string itemId, float price, Action onOk, Action onFailed)
        {
            var itemInfo = PublisherSettings.I.limited_items.FirstOrDefault(x => x.GameUid == itemId);
            if (itemInfo == null)
            {
                throw new InvalidOperationException($"Failed to find item with id {itemId}");
            }

            var token = PublisherSettings.I.Tokens.First(x => x.Mint == itemInfo.PriceMint || x.DevMint == itemInfo.PriceMint);
            ulong rawPrice = (ulong)(price * Math.Pow(10, token.Decimals)) ;

            RunTx(() => $"https://cldfn.com/matosolana/project/{PublisherSettings.I.GameWalletPubkey}/market/list/{itemInfo.Mint}/{PhantomController.Pubkey}?price_mint={itemInfo.PriceMint}&price={rawPrice}", onOk, onFailed);
        }

        public static void BuyMarketItem(string gameId, string listingAddress, Action onOk, Action onFailed)
        {
            RunTx(() => $"https://cldfn.com/matosolana/project/{PublisherSettings.I.GameWalletPubkey}/market/buy/{listingAddress}/{PhantomController.Pubkey}", onOk, onFailed);
        }

        private static void RunTx(Func<string> getUrl, Action onOk, Action onFailed)
        {
            Action action = () =>
            {
                var url = getUrl();
                
                Timing.RunCoroutine(GetRequest(url, jsonStr =>
                {
                    var res = JsonConvert.DeserializeObject<CreateTxResponse>(jsonStr);

                    var rawTx = Convert.FromBase64String(res.tx);
                    PhantomController.SignAndSendTransaction(rawTx, txSig =>
                    {
                        ConfirmTx(txSig, 120.0f, () =>
                        {
                            onOk?.Invoke();
                        }, () =>
                        {
                            Debug.Log("Failed to confirm tx");
                            onFailed?.Invoke();
                        });
                    }, onFailed);
                }, onFailed));
            };
            
            if (PhantomController.IsConnected)
            {
                action();
            }
            else
            {
                PhantomController.Connect(wallet =>
                {
                    Timing.CallDelayed(0.2f, action);
                }, onFailed);
            }
        }

        private static IEnumerator<float> GetRequest(string url, Action<string> onOk, Action onError)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return Timing.WaitUntilDone(webRequest.SendWebRequest());

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Failed request");
                        onError?.Invoke();
                        break;
                    case UnityWebRequest.Result.Success:
                        onOk?.Invoke(webRequest.downloadHandler.text);
                        break;
                }
            }
        }

        public static string CatalogUrl =>
            $"https://cldfn.com/matosolana/project/{PublisherSettings.I.GameWalletPubkey}/items";

        public static string MarketItemsUrl =>
            $"https://cldfn.com/matosolana/project/{PublisherSettings.I.GameWalletPubkey}/market/items";
        
        public static void FetchMarketItems(Action<IReadOnlyList<MarketItem>> onOk, Action onFailed)
        {
            Timing.RunCoroutine(GetRequest(MarketItemsUrl, res =>
            {
                var marketData = JsonConvert.DeserializeObject<MarketData>(res);
                var items = marketData.Items.Where(x =>
                    PublisherSettings.I.limited_items.FirstOrDefault(item => item.Mint == x.Mint && item.IsEnabled) !=
                    null).ToArray();
                onOk?.Invoke(items);
            }, onFailed));
        }

        private static List<UserItem> _userInventoryCache;

        public static IReadOnlyList<UserItem> UserInventory => _userInventoryCache;

        public static void FetchUserItems(Action<IReadOnlyList<UserItem>> onOk, Action onFailed)
        {
            if (!PhantomController.IsConnected)
            {
                PhantomController.Connect(s =>
                {
                    Timing.RunCoroutine(_GetUserBalances(onOk, onFailed));
                }, onFailed);
            }
            else
            {
                Timing.RunCoroutine(_GetUserBalances(onOk, onFailed));
            }
        }

        private static IEnumerator<float> _GetUserBalances(Action<IReadOnlyList<UserItem>> onOk, Action onFailed)
        {
            var walletPubkey = PhantomController.Pubkey;

            var task = SolanaApi.GetTokenAccounts(walletPubkey);
            var time = DateTime.Now;

            while ((DateTime.Now - time).TotalSeconds < 20)
            {
                if (!task.IsCompleted)
                {
                    yield return Timing.WaitForOneFrame;
                }
                else
                {
                    var res = new List<UserItem>();
                    var taskRes = task.Result;
                    foreach (var accountData in taskRes)
                    {
                        if (accountData.Amount == 0)
                        {
                            continue;
                        }
                        
                        var catalogItem =
                            PublisherSettings.I.limited_items.FirstOrDefault(x => x.Mint == accountData.Mint);
                        
                        if (catalogItem != null && catalogItem.IsEnabled)
                        {
                            res.Add(new UserItem()
                            {
                                Count = (int)accountData.Amount,
                                GameUid = catalogItem.GameUid,
                                IsOnSale = false
                            });
                        }
                    }

                    _userInventoryCache = res;
                    onOk?.Invoke(res);
                    yield break;
                }
            }
            
            onFailed?.Invoke();
        }

        public static void ConfirmTx(string signature, float timeoutSeconds, Action onOk, Action onFailed)
        {
            Timing.RunCoroutine(_ConfirmTxRoutine(signature, timeoutSeconds, onOk, onFailed));
        }

        private static IEnumerator<float> _ConfirmTxRoutine(string signature, float timeoutSeconds, Action onOk, Action onFailed)
        {
            var time = DateTime.Now;

            while ((DateTime.Now - time).TotalSeconds < timeoutSeconds)
            {
                var checkTask = SolanaApi.GetSignatureStatus(signature);
                
                while (!checkTask.IsCompleted)
                {
                    yield return Timing.WaitForOneFrame;
                }

                if (checkTask.Result != null)
                {
                    var res = checkTask.Result;

                    if (res.Confirmations.HasValue)
                    {
                        var progress =  (int)((res.Confirmations.Value / 32f) * 100);
                        Debug.Log($"Confirming: {progress}%");
                    }
                    else
                    {
                        if (res.IsFinalized)
                        {
                            onOk?.Invoke();
                            yield break;
                        }
                        
                        Debug.Log($"Processing transaction...");
                    }
                }
                
                yield return Timing.WaitForSeconds(1);
            }
            
            onFailed?.Invoke();
        }

    }
}