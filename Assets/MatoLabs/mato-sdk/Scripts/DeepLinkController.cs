using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatoLabs.mato_sdk.Scripts
{
    public class UrlData
    {
        public string Method { get; private set; }
        public readonly Dictionary<string, string> Params = new();

        public static UrlData Parse(string url)
        {
            var res = new UrlData();
            
            var method = string.Empty;
            
            var body = url.Split("//")[1];
            if (!body.Contains('?'))
            {
                res.Method = body;
                return res;
            }

            var parts = body.Split('?');
            method = parts[0];
            res.Method = method;

            var paramVals = parts[1].Split('&');
            foreach (var paramVal in paramVals)
            {
                var pts = paramVal.Split('=');
                res.Params.Add(pts[0], pts[1]);
            }
            
            return res;
        }

        public override string ToString()
        {
            var res = $"Method: {this.Method}";

            foreach (var param in this.Params)
            {
                res += $"\nKey: {param.Key}, val: {param.Value}";
            }
            
            return res;
        }
    }
    
    public static class DeepLinkController
    {
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        public static Action<UrlData> OnDeepLinkReceived = data => { };

        private static void OnDeepLinkActivated(string url)
        {
            Debug.Log("Link received: " + url);
            OnDeepLinkReceived.Invoke(UrlData.Parse(url));
        }
    }

}