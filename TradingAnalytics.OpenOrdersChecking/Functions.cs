using BinanceExchange.API.Extensions;
using BinanceExchange.API.Models.Response;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.IO;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.Application.Services;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.OpenOrdersChecking
{
    public class Functions
    {
        public async System.Threading.Tasks.Task ProcessMethodAsync([TimerTrigger("00:00:30", RunOnStartup = true)] TimerInfo timerInfo, TextWriter log)
        {
            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();

            List<Order> openOrders = tradingServices.GetOpenOrders();

            foreach (var order in openOrders)
            {
                string clientOrderId = (order.BuyStatus != "FILLED" && order.BuyStatus != "CANCELED") ? order.BuyClientOrderId : order.SellClientOrderId;

                OrderResponse openOrder = await binanceService.GetOrder(order.BaseAsset + order.QuoteAsset, clientOrderId);

                decimal lastBaseAssetPrice = await binanceService.GetCurrentPrice(openOrder.Symbol);

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
                }
            }
        }
    }
}
