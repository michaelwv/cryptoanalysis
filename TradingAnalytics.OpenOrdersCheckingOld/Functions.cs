using BinanceExchange.API.Enums;
using BinanceExchange.API.Extensions;
using BinanceExchange.API.Models.Response;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.Application.Services;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.OpenOrdersCheckingOld
{
    public class Functions
    {
        public bool ProcessMethodAsync([TimerTrigger("00:01:00", RunOnStartup = true)] TimerInfo timerInfo, TextWriter log)
        {
            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();

            List<Order> openOrders = tradingServices.GetOpenOrders();

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

                if ((lastBaseAssetPrice >= price && orderSide == "SELL") || (lastBaseAssetPrice <= price && orderSide == "BUY"))
                {
                    tradingServices.UpdateOrderStatus(clientOrderId, orderSide, "FILLED", lastBaseAssetPrice);

                    string message = orderSide + " ORDER FILLED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + price + " - Quantity: " + order.Quantity;

                    Console.WriteLine(message);

                    TelegramService telegramService = new TelegramService();
                    HttpResponseMessage response = telegramService.SendMessageAsync(message).Result;
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
    }
}
