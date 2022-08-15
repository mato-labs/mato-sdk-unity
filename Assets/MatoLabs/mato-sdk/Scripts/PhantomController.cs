using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;
using AllArt.Solana;
using Chaos.NaCl;
using MatoLabs.mato_sdk.Scripts.Scriptables;
using MEC;
using Merkator.BitCoin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MatoLabs.mato_sdk.Scripts
{
    [Serializable]
    public class PhantomConnectData
    {
        [JsonProperty("public_key")]
        public string WalletPublicKey { get; private set; }
        
        [JsonProperty("session")]
        public string Session { get; private set; }
    }
    
    public static class PhantomController
    {

        private const string BaseUrl = "https://phantom.app/ul/v1";
        private const string ConnectMethod = "connect";
        private const string DisconnectMethod = "disconnect";
        private const string SignMessageMethod = "signMessage";
        private const string SignAndSendTxMethod = "signAndSendTransaction";
        private const string SignTxMethod = "signTransaction";

        private const string AppUrl = "https://matolabs.com";
        private static string AppUrlScheme = "matosdk://";
        
        private const string ConnectCallbackUrl = "onPhantomConnected";
        private const string DisconnectCallbackUrl = "onPhantomDisconnected";
        private const string SignMessageCallbackUrl = "onMessageSigned";
        private const string SignAndSendTxCallbackUrl = "onTxSignedAndSent";
        private const string SignTxCallbackUrl = "onTxSigned";

        // Session data
        private static Keypair _sessionKeypair;
        private static PhantomConnectData _connectData;
        private static byte[] _sharedSecret;

        private static Action _errorHandler;
        private static Action<string> _onOk;
        private static Action _onOkNoParams;
        
        public static string Pubkey { get; private set; }

        //mainnet-beta
        //testnet
        //devnet
        private const string Clustser = "devnet";
        
        public static bool IsConnected { get; private set; }
        public static bool AutoConnect { get; set; } = false;
        
        static PhantomController()
        {
            AppUrlScheme = $"{PublisherSettings.I.SchemePrefix}://";
            
            DeepLinkController.OnDeepLinkReceived += data =>
            {
                if (HasError(data))
                {
                    _errorHandler?.Invoke();
                    ResetState();
                    return;
                }
                
                if (data.Method == ConnectCallbackUrl)
                {
                    OnConnectResponse(data);
                }
                
                if (data.Method == DisconnectCallbackUrl)
                {
                    IsConnected = false;
                    _onOkNoParams?.Invoke();
                }
                
                if (data.Method == SignMessageCallbackUrl)
                {
                    OnMessageSigned(data);
                }
                
                if (data.Method == SignAndSendTxCallbackUrl)
                {
                    OnTxSignedAndSent(data);
                }
                
                if (data.Method == SignTxCallbackUrl)
                {
                    OnTxSigned(data);
                }
                
                
                ResetState();
            };
        }

        private static void ResetState()
        {
            _errorHandler =  null;
            _onOk = null;
            _onOkNoParams = null;
        }

        private static bool HasError(UrlData data)
        {
            if (data.Params.ContainsKey("errorCode"))
            {
                _errorHandler?.Invoke();
                return true;
            }

            return false;
        }

        private static string DecryptPayload(string payload, byte[] nonce)
        {
            var encodedData = Base58Encoding.Decode(payload);
            var decryptedData = XSalsa20Poly1305.TryDecrypt(encodedData, _sharedSecret, nonce);
            string decryptedStr = System.Text.Encoding.UTF8.GetString(decryptedData);;

            return decryptedStr;
        }
        
        private static (string, string) EncryptPayload(Dictionary<string, string> payload)
        {
            var payloadStr = JsonConvert.SerializeObject(payload);
            Debug.Log("Payload str: " + payloadStr);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payloadStr);
            var nonce = GetRandomBytes(24);

            var encryptedBytes = XSalsa20Poly1305.Encrypt(payloadBytes, _sharedSecret, nonce);
            return (Base58Encoding.Encode(encryptedBytes), Base58Encoding.Encode(nonce));
        }
        
        private static void OnConnectResponse(UrlData data)
        {
            var phantom_pubkey = Base58Encoding.Decode(data.Params["phantom_encryption_public_key"]);
            var nonce = Base58Encoding.Decode(data.Params["nonce"]);
            _sharedSecret = MontgomeryCurve25519.KeyExchange(phantom_pubkey, _sessionKeypair.privateKeyByte);
            
            var decryptedStr = DecryptPayload(data.Params["data"], nonce);
            _connectData = JsonConvert.DeserializeObject<PhantomConnectData>(decryptedStr);

            IsConnected = true;
            Pubkey = _connectData.WalletPublicKey;
            _onOk.Invoke(_connectData.WalletPublicKey);
        }

        private static void OnMessageSigned(UrlData data)
        {
            var nonce = Base58Encoding.Decode(data.Params["nonce"]);
            var decryptedStr = DecryptPayload(data.Params["data"], nonce);
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedStr);
            var signature = dataDic["signature"];

            _onOk?.Invoke(signature);
        }

        public static bool VerifySignature(string msg, string signature)
        {
            #if UNITY_EDITOR
            return Ed25519.Verify(Base58Encoding.Decode(signature), System.Text.Encoding.UTF8.GetBytes(msg), _sessionKeypair.publicKeyByte);
            #endif
            
            return Ed25519.Verify(Base58Encoding.Decode(signature), System.Text.Encoding.UTF8.GetBytes(msg), Base58Encoding.Decode(_connectData.WalletPublicKey));
        }

        public static byte[] GetRandomBytes(int length)
        {
            var randomBytes = new byte[length];
            var rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(randomBytes);

            return randomBytes;
        }

        private static Keypair GenerateKeypair()
        {
#if UNITY_EDITOR
            var seed = GetRandomBytes(32);
            byte[] pk;
            byte[] pbk;
            
            Ed25519.KeyPairFromSeed(out pbk, out pk, seed);
            return new Keypair(pbk, pk);
#endif
            var privateKey = GetRandomBytes(32);
            var pubkey = MontgomeryCurve25519.GetPublicKey(privateKey);

            return new Keypair(pubkey, privateKey);;
        }
        private static string GetCallbackUrl(string method)
        {
            return Encode($"{AppUrlScheme}{method}");
        }

        private static string Encode(string url)
        {
            return UnityWebRequest.EscapeURL(url);
        }
        
        public static void Connect(Action<string> onOk, Action errorHandler)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Wallet already connected");
            }
            
            _sessionKeypair = GenerateKeypair();
            _errorHandler = errorHandler;
            _onOk = onOk;
            
#if UNITY_EDITOR
            IsConnected = true;
            _onOk?.Invoke(Base58Encoding.Encode(_sessionKeypair.publicKeyByte));
            return;
#endif
            
            var connectUrl = $"{BaseUrl}/{ConnectMethod}?app_url={Encode(AppUrl)}&redirect_link={GetCallbackUrl(ConnectCallbackUrl)}&dapp_encryption_public_key={_sessionKeypair.publicKey}&cluster={Clustser}";
            
            Application.OpenURL(connectUrl);
        }

       

        public static void Disconnect(Action onOk, Action errorHandler)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Wallet not connected");
            }
            
            _errorHandler = errorHandler;
            _onOkNoParams = onOk;
            
#if UNITY_EDITOR
            IsConnected = false;
            _onOkNoParams?.Invoke();
            return;
#endif
            
            var payload = new Dictionary<string, string>();
            payload.Add("session", _connectData.Session);

            var (encryptedPayload, nonce) = EncryptPayload(payload);
            
            var url = $"{BaseUrl}/{DisconnectMethod}?redirect_link={GetCallbackUrl(DisconnectCallbackUrl)}&dapp_encryption_public_key={_sessionKeypair.publicKey}&nonce={nonce}&payload={encryptedPayload}";
            
            Application.OpenURL(url);
        }

        public static void SignMessage(string msg, Action<string> onComplete, Action onError)
        {
            if (!AutoConnect && !IsConnected)
            {
                throw new InvalidOperationException("Wallet not connected");
            }

            Action loginAction = () =>
            {
                _errorHandler = onError;
                _onOk = onComplete;
            
#if UNITY_EDITOR
                var sig = Ed25519.Sign(System.Text.Encoding.UTF8.GetBytes(msg), _sessionKeypair.privateKeyByte);
                _onOk?.Invoke(Base58Encoding.Encode(sig));
                return;
#endif
                var payload = new Dictionary<string, string>();
                payload.Add("session", _connectData.Session);
                payload.Add("message", Base58Encoding.Encode(System.Text.Encoding.UTF8.GetBytes(msg)));
            
                var (encryptedPayload, nonce) = EncryptPayload(payload);
                var url = $"{BaseUrl}/{SignMessageMethod}?redirect_link={GetCallbackUrl(SignMessageCallbackUrl)}&dapp_encryption_public_key={_sessionKeypair.publicKey}&nonce={nonce}&payload={encryptedPayload}";
            
                Application.OpenURL(url);
            };
            
            if (AutoConnect && !IsConnected)
            {
                Connect(s =>
                {
                    Timing.CallDelayed(0.1f, loginAction);
                }, onError);
            }
            else
            {
                loginAction();
            }
        }
        
        
        public static void SignAndSendTransaction(byte[] serializedTx, Action<string> onOk, Action onError)
        {
            if (!AutoConnect && !IsConnected)
            {
                throw new InvalidOperationException("Wallet not connected");
            }

            Action fnAction = () =>
            {
                _errorHandler = onError;
                _onOk = onOk;
            
                var payload = new Dictionary<string, string>();
                payload.Add("session", _connectData.Session);
                payload.Add("transaction", Base58Encoding.Encode(serializedTx));
            
                var (encryptedPayload, nonce) = EncryptPayload(payload);
            
                var url = $"{BaseUrl}/{SignAndSendTxMethod}?redirect_link={GetCallbackUrl(SignAndSendTxCallbackUrl)}&dapp_encryption_public_key={_sessionKeypair.publicKey}&nonce={nonce}&payload={encryptedPayload}";
            
                Application.OpenURL(url);
            };
            
            if (AutoConnect && !IsConnected)
            {
                Connect(s =>
                {
                    Timing.CallDelayed(0.1f, fnAction);
                }, onError);
            }
            else
            {
                fnAction();
            }
        }


        private static void OnTxSignedAndSent(UrlData data)
        {
            var nonce = Base58Encoding.Decode(data.Params["nonce"]);
            var decryptedStr = DecryptPayload(data.Params["data"], nonce);
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedStr);
            var signature = dataDic["signature"];

            _onOk?.Invoke(signature);
        }

        public static void SignTransaction(byte[] serializedTx, Action<string> onOk, Action onError)
        {
            if (!AutoConnect && !IsConnected)
            {
                throw new InvalidOperationException("Wallet not connected");
            }

            Action fnAction = () =>
            {
                _errorHandler = onError;
                _onOk = onOk;

                var payload = new Dictionary<string, string>();
                payload.Add("session", _connectData.Session);
                payload.Add("transaction", Base58Encoding.Encode(serializedTx));

                var (encryptedPayload, nonce) = EncryptPayload(payload);

                var url =
                    $"{BaseUrl}/{SignTxMethod}?redirect_link={GetCallbackUrl(SignTxCallbackUrl)}&dapp_encryption_public_key={_sessionKeypair.publicKey}&nonce={nonce}&payload={encryptedPayload}";

                Application.OpenURL(url);
            };

            if (AutoConnect && !IsConnected)
            {
                Connect(s =>
                {
                    Timing.CallDelayed(0.1f, fnAction);
                }, onError);
            }
            else
            {
                fnAction();
            }
        }
        
        private static void OnTxSigned(UrlData data)
        {
            var nonce = Base58Encoding.Decode(data.Params["nonce"]);
            var decryptedStr = DecryptPayload(data.Params["data"], nonce);
            var dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedStr);
            var signature = dataDic["transaction"];

            _onOk?.Invoke(signature);
        }
    }
}















