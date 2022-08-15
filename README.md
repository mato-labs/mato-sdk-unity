# Mato SDK for Unity

With Mato SDK for Unity you can easily add on-chain collectibles,  limited-edition items and NFTs to your game in minutes.



**Visit our [website](https://matolabs.com/) for more info.**


## Features
- **Web dashboard** to manage shop items and check sales and revenue analytics
- **Clean and simple API**
- **Integrated P2P marketplace**
- **Pre-built market view prefabs** - just replace sprites and you are good to go!
- **Mobile wallet adapter** - currently only Phantom supports mobile integration. As soon as other wallets add it - they will be supported by our SDK


## License

[MIT](https://choosealicense.com/licenses/mit/)


## Documentation

[Documentation](https://docs.matolabs.com/mato-sdk-for-unity)


## API Reference

#### Byy item fom the shop

```csharp
  void MatoSDK.BuyItem(string itemId, Action onOk, Action onFailed);
```

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `itemId`  | `string` | **Required**. Game id that you assigned to item in the dashboard |


#### Buy item from the market

```csharp
  void MatoSDK.BuyMarketItem(string gameId, string listingAddress, Action onOk, Action onFailed);
```


#### List item on the market

```csharp
  void MatoSDK.ListItem(string itemId, float price, Action onOk, Action onFailed);
```


#### Unlist item from the market

```csharp
  void MatoSDK.UnlistItem(string itemId, float price, Action onOk, Action onFailed);
```


#### Get a list of market items

```csharp
  void MatoSDK.FetchMarketItems(Action<IReadOnlyList<MarketItem>> onOk, Action onFailed);
```

#### Get user inventory

```csharp
  void MatoSDK.FetchUserItems(Action<IReadOnlyList<UserItem>> onOk, Action onFailed);
```


