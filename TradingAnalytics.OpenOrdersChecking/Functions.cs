using BinanceExchange.API.Models.Response;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using TradingAnalytics.Application.Services;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.OpenOrdersChecking
{
    public class Functions
    {
        public void CheckOpenOrders()
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            logger.Debug("-----------------Process Started------------------");
            Console.WriteLine("-----------------Process Started------------------");

            try
            {
                BinanceService binanceService = new BinanceService();
                TradingServices tradingServices = new TradingServices();

                List<Order> openOrders = tradingServices.GetOpenOrders();

                if (openOrders.Count == 0)
                {
                    logger.Debug("No order found.");
                    Console.WriteLine("No order found.");
                }
                else
                {

                    foreach (var order in openOrders)
                    {
                        string clientOrderId = "";
                        string orderSide = "";
                        decimal price = 0;

                        if (order.BuyStatus == "NEW")
                        {
                            clientOrderId = order.BuyClientOrderId;
                            price = order.BuyPrice;
                            orderSide = "BUY";
                        }
                        else
                        {
                            clientOrderId = order.SellClientOrderId;
                            price = order.SellPrice;
                            orderSide = "SELL";
                        }

                        decimal lastBaseAssetPrice = binanceService.GetCurrentPrice(order.BaseAsset + order.QuoteAsset).Result;

                        logger.Debug(order.BaseAsset + order.QuoteAsset + " current price: " + lastBaseAssetPrice.ToString());

                        decimal quoteAssetPriceInDollars = Math.Round(binanceService.GetCurrentPrice(SettingsService.GetQuoteAssetToTrade() + "USDT").Result, 2);

                        if ((lastBaseAssetPrice >= price && orderSide == "SELL") || (lastBaseAssetPrice <= price && orderSide == "BUY"))
                        {
                            tradingServices.UpdateOrderStatus(clientOrderId, orderSide, "FILLED", lastBaseAssetPrice, quoteAssetPriceInDollars);

                            string message = orderSide + " ORDER FILLED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + price + " - Quantity: " + order.Quantity;
                            logger.Debug(message);

                            Console.WriteLine(message);

                            TelegramService telegramService = new TelegramService();
                            HttpResponseMessage response = telegramService.SendMessageAsync(message).Result;
                        }
                        else
                        {
                            tradingServices.UpdateLastPrice(clientOrderId, orderSide, lastBaseAssetPrice);

                            OrderBookResponse orderBook = binanceService.GetOrderBook(order.BaseAsset + order.QuoteAsset).Result;

                            if (orderSide == "BUY")
                            {
                                if (!tradingServices.LowerWallStillExists(orderBook, order.AssetPrecision, quoteAssetPriceInDollars, order.BuyPrice))
                                {
                                    tradingServices.UpdateOrderStatus(clientOrderId, orderSide, "CANCELED", lastBaseAssetPrice, quoteAssetPriceInDollars);

                                    string message = orderSide + " ORDER CANCELED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + price + " - Quantity: " + order.Quantity;
                                    logger.Debug(message);

                                    Console.WriteLine(message);

                                    //TelegramService telegramService = new TelegramService();
//                                    HttpResponseMessage response = telegramService.SendMessageAsync(message).Result;
                                }
                            }
                            else
                            {
                                decimal sellWallPrice = 0;

                                if (tradingServices.UpperWallFormedBeforeDesiredProfit(orderBook, quoteAssetPriceInDollars, order.BuyPrice, order.SellPrice, out sellWallPrice))
                                {
                                    tradingServices.UpdateSellOrderUrderWall(clientOrderId, order.AssetPrecision, sellWallPrice);

                                    string message = "SELL WALL FOUND: " + order.BaseAsset + order.QuoteAsset + " - Wall Price: " + sellWallPrice + ". ORDER UPDATED.";
                                    logger.Debug(message);

                                    Console.WriteLine(message);

                                    TelegramService telegramService = new TelegramService();
                                    HttpResponseMessage response = telegramService.SendMessageAsync(message).Result;
                                }
                                else
                                {
                                    decimal newSellPrice = tradingServices.GetSellPrice(orderBook, order.BuyPrice, order.AssetPrecision, quoteAssetPriceInDollars, true);

                                    if (newSellPrice > order.BuyPrice && newSellPrice != order.SellPrice)
                                    {
                                        tradingServices.UpdateSellOrderPrice(clientOrderId, newSellPrice);

                                        string message = "SELL PRICE UPDATED: " + order.BaseAsset + order.QuoteAsset + " - Sell Price: " + newSellPrice + ". ORDER UPDATED.";
                                        logger.Debug(message);

                                        Console.WriteLine(message);

                                        TelegramService telegramService = new TelegramService();
                                        HttpResponseMessage response = telegramService.SendMessageAsync(message).Result;
                                    }
                                }
                            }
                        }

                        /*OrderResponse openOrder = await binanceService.GetOrder(order.BaseAsset + order.QuoteAsset, clientOrderId);

                        tradingServices.UpdateOrderStatus(clientOrderId, EnumExtensions.GetEnumMemberValue(openOrder.Side), EnumExtensions.GetEnumMemberValue(openOrder.Status), lastBaseAssetPrice);

                        if (openOrder.Status == BinanceExchange.API.Enums.OrderStatus.New)
                        {
                            OrderBookResponse orderBook = await binanceService.GetOrderBook(openOrder.Symbol);

                            if (openOrder.Side == BinanceExchange.API.Enums.OrderSide.Buy)
                            {
                                if (!tradingServices.LowerWallStillExists(orderBook))
                                    binanceService.CancelOrder(openOrder.Symbol, clientOrderId);
                            }
                            else
                            {
                                if (tradingServices.UpperWallFormedBeforeDesiredProfit(orderBook))
                                    binanceService.UpdateSellOrderUrderWall(openOrder.Symbol, clientOrderId);
                            }
                        }
                        else if (openOrder.Status == BinanceExchange.API.Enums.OrderStatus.Filled)
                        {
                            if (openOrder.Side == BinanceExchange.API.Enums.OrderSide.Buy)
                                binanceService.SetSalesOrder(openOrder.Symbol, order.SellPrice);
                        }*/
                    }
                }

                logger.Debug("-----------------Process Finished-----------------");
                Console.WriteLine("-----------------Process Finished-----------------");
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));

                TelegramService telegramService = new TelegramService();
                HttpResponseMessage response = telegramService.SendMessageAsync(e.Message).Result;
            }
        }
    }
}
