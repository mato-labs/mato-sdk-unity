using System;
using System.Collections;
using MatoLabs.mato_sdk.Scripts.Models;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace MatoLabs.mato_sdk.Scripts.Scriptables
{
    [CreateAssetMenu(fileName = "PublisherSettings", menuName = "MatoSDK/PublisherSettings", order = 1)]
    public class PublisherSettings: ScriptableObject
    {
        public string GameWalletPubkey;
        public string SchemePrefix;

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium)]
        [PropertyOrder(-1)]
        public void FetchServerData()
        {
            var addr = PublisherSettings.I.GameWalletPubkey;
            var url = MatoSdk.CatalogUrl;

            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(GetRequest(MatoSdk.CatalogUrl, res =>
            {
                var data = JsonConvert.DeserializeObject<CatalogData>(res);
                limited_items = data.Items;
                
                UnityEditor.AssetDatabase.SaveAssets();
            }), this);
        }

#endif
        public string MainnetUrl;
        public string DevnetUrl;
        public bool DevNet = false;
        public LimitedItem_v2[] limited_items;

        public TokenMeta[] Tokens;

        public string ClusterUrl => DevNet ? DevnetUrl : MainnetUrl;
        
        private static PublisherSettings _i;

        public static PublisherSettings I
        {
            get
            {
                if (_i == null)
                {
                    _i = Resources.Load<PublisherSettings>("Settings");
                }

                return _i;
            }
        }
        
        
        private IEnumerator GetRequest(string url, Action<string> onOk)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Failed request");
                        break;
                    case UnityWebRequest.Result.Success:
                        onOk?.Invoke(webRequest.downloadHandler.text);
                        break;
                }
            }
        }
    }
}