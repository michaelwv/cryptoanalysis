using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Extensions;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.DataAccess;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.Application.Services
{
    public class BinanceService
    {
        public async Task<List<ExchangeInfoSymbol>> GetTradingCoins()
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                ExchangeInfoResponse exchangeInfo = await client.GetExchangeInfo();

                if (exchangeInfo != null)
                {
                    if (exchangeInfo.Symbols.Count > 0)
                        return exchangeInfo.Symbols.FindAll(x => x.Status == "TRADING" && x.QuoteAsset == SettingsService.GetQuoteAssetToTrade());
                    else
                        throw new Exception("Unexpected error when getting trading coins.", null);
                }
                else
                    throw new Exception("Unexpected error when getting trading coins.", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public void CancelOrder(string symbol, string clientOrderId)
        {
            throw new NotImplementedException();
        }

        public async Task<OrderResponse> GetOrder(string symbol, string clientOrderId)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                OrderResponse order = await client.QueryOrder(new QueryOrderRequest()
                {
                    Symbol = symbol,
                    OriginalClientOrderId = clientOrderId
                });

                if (order != null)
                    return order;
                else
                    throw new Exception("Unexpected error when getting open order for symbol " + symbol + ".", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public void SetSalesOrder(string symbol, decimal sellPrice)
        {
            throw new NotImplementedException();
        }

        public void UpdateSellOrderUrderWall(string symbol, string clientOrderId)
        {
            throw new NotImplementedException();
        }

        public async Task<OrderBookResponse> GetOrderBook(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                OrderBookResponse orderBook = await client.GetOrderBook(symbol, false, 10);

                if (orderBook != null)
                    return orderBook;
                else
                    throw new Exception("Unexpected error when getting order book.", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<SymbolPriceChangeTickerResponse> GetDailyTicker(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                SymbolPriceChangeTickerResponse dailyTicker = await client.GetDailyTicker(symbol);

                if (dailyTicker != null)
                    return dailyTicker;
                else
                    throw new Exception("Unexpected error when getting order book.", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<decimal> GetCurrentPrice(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await httpClient.GetAsync(SettingsService.GetBinanceEndPoint() + "api/v3/ticker/price?symbol=" + symbol);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonMessage = response.Content.ReadAsStringAsync();
                        var price = JsonConvert.DeserializeObject<SymbolPriceResponse>(jsonMessage.Result);
                        return price.Price;
                    }
                    else
                    {
                        throw new Exception("Unexpected error when getting price ticker.", null);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public /*async Task<*/bool/*>*/ SetNewOrder(TradeOpportunityDTO tradeOpportunity, OrderSide side, OrderType type)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                OrderRepository orderRepository = new OrderRepository();
                string[] pendingStatus = new string[] { EnumExtensions.GetEnumMemberValue(OrderStatus.New), EnumExtensions.GetEnumMemberValue(OrderStatus.PartiallyFilled) };

                if (orderRepository.PendingOrderExists(tradeOpportunity.BaseAsset, tradeOpportunity.QuoteAsset, pendingStatus))
                    return false;

                TradingServices tradingServices = new TradingServices();
                decimal price = (side == OrderSide.Buy) ? tradeOpportunity.BuyPrice : tradeOpportunity.SellPrice;
                decimal quantity = tradingServices.GetOrderQuantity(tradeOpportunity.BaseAssetPriceInUsd, tradeOpportunity.MinQty, tradeOpportunity.MaxQty);

                if (quantity > 0)
                {
                    //var logger = LogManager.GetLogger(typeof(BinanceService));

                    //SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                    //var client = new BinanceClient(new ClientConfiguration()
                    //{
                    //    ApiKey = binanceKeys.ApiKey,
                    //    SecretKey = binanceKeys.SecretKey,
                    //    Logger = logger,
                    //});

                    //var binanceResult = await client.CreateOrder(new CreateOrderRequest()
                    //{
                    //    Symbol = tradeOpportunity.BaseAsset + tradeOpportunity.QuoteAsset,
                    //    Side = side,
                    //    Type = type,
                    //    Quantity = quantity,
                    //    Price = price,
                    //    NewOrderResponseType = NewOrderResponseType.Result
                    //});

                    //if (binanceResult.ClientOrderId != null)
                    //{
                    Order order = new Order()
                    {
                        BaseAsset = tradeOpportunity.BaseAsset,
                        QuoteAsset = tradeOpportunity.QuoteAsset,
                        AssetPrecision = tradeOpportunity.BaseAssetPrecision,
                        Quantity = quantity,
                        BuyPrice = tradeOpportunity.BuyPrice,
                        BuyStatus = EnumExtensions.GetEnumMemberValue(OrderStatus.New),
                        //BuyClientOrderId = binanceResult.ClientOrderId,
                        BuyIncDate = DateTime.Now,
                        QuoteAssetPriceAtBuy = tradeOpportunity.QuoteAssetPriceInUsd,
                        SellPrice = tradeOpportunity.SellPrice,
                        LastPrice = tradeOpportunity.LastBaseAssetPrice,
                        LastPriceDate = DateTime.Now                        
                    };

                    if (orderRepository.CreateOrder(order) > 0)
                    {
                        #if DEBUG
                            orderRepository.UpdateClientOrderId();
                        #endif
                        return true;
                    }
                    else
                        throw new Exception("Unable to create new order.");
                    //}
                    //else
                    //    throw new Exception("Unable to create new order.");
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                logger.Debug(JsonConvert.SerializeObject(e));
                throw e;
            }
        }
    }
}

