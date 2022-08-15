using System.Collections;
using System.Collections.Generic;
using MatoLabs.mato_sdk.Scripts;
using MEC;
using Slash.Unity.DataBind.Core.Data;
using UnityEngine;
using UnityEngine.Networking;
using Chaos.NaCl;
using Merkator.BitCoin;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

public class DemoWalletAdapterContext : Context
{
    #region IsConnecting Property

    private readonly Property<bool> isConnectingProperty = new Property<bool>();

    public bool IsConnecting
    {
        get { return isConnectingProperty.Value; }
        set { isConnectingProperty.Value = value; }
    }

    #endregion


    #region WalletPubkey Property

    private readonly Property<string> walletPubkeyProperty = new Property<string>();

    public string WalletPubkey
    {
        get { return walletPubkeyProperty.Value; }
        set { walletPubkeyProperty.Value = value; }
    }

    #endregion


    #region Status Property

    private readonly Property<string> statusProperty = new Property<string>();

    public string Status
    {
        get { return statusProperty.Value; }
        set { statusProperty.Value = value; }
    }

    #endregion

    #region IsConnected Property

    private readonly Property<bool> isConnectedProperty = new Property<bool>();

    public bool IsConnected
    {
        get { return isConnectedProperty.Value; }
        set { isConnectedProperty.Value = value; }
    }

    #endregion


    public DemoWalletAdapterContext()
    {
        PhantomController.AutoConnect = true;
    }

    public void ConnectCommand()
    {
        PhantomController.Connect((wallet) =>
            {
                WalletPubkey = wallet;
                IsConnected = true;
            },
() => Status = "Failed to connect");
    }

    public void DisconnectCommand()
    {
        PhantomController.Disconnect(() =>
        {
            WalletPubkey = string.Empty;
            IsConnected = false;
        }, () => Status = "Failed to disconnect");
    }

    public void SignMessageCommand()
    {
        const string msg = "hello, world!";
        
        PhantomController.SignMessage(msg, sig =>
        {
            Status = $"Signature: {sig}, verified: {PhantomController.VerifySignature(msg, sig)}";
        }, () =>
        {
            Status = "Failed to sign mesasge";
        });
    }
    

   

}
