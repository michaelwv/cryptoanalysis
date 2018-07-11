using BinanceExchange.API.Models.Response;
using System;
using System.Collections.Generic;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.DataAccess;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.Application.Services
{
    public class TradingServices
    {
        public TradeOpportunityDTO ValidateTradeOpportunity(OrderBookResponse orderBook, decimal lastBaseAssetPrice, decimal baseAssetPriceInDollars, decimal quoteAssetPriceInDollars, int assetPrecision, string baseAsset, string quoteAsset)
        {
            decimal buyPrice = GetBuyPrice(orderBook, assetPrecision, quoteAssetPriceInDollars);

            if (buyPrice == 0)
                return null;

            decimal sellPrice = GetSellPrice(orderBook, buyPrice, assetPrecision, quoteAssetPriceInDollars);

            if (buyPrice > 0 && sellPrice > 0)
            {
                return new TradeOpportunityDTO()
                {
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    BaseAsset = baseAsset,
                    QuoteAsset = quoteAsset,
                    LastBaseAssetPrice = lastBaseAssetPrice,
                    BaseAssetPriceInUsd = baseAssetPriceInDollars,
                    BaseAssetPrecision = assetPrecision,
                    OrderBook = orderBook
                };
            }
            else
                return null;
        }

        public int UpdateOrderStatus(string clientOrderId, string side, string status, decimal lastPrice)
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.UpdateOrderStatus(clientOrderId, side, status, lastPrice);
        }

        public bool LowerWallStillExists(OrderBookResponse orderBook)
        {
            throw new NotImplementedException();
        }

        public List<Order> GetOpenOrders()
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.GetOpenOrders();
        }

        public bool UpperWallFormedBeforeDesiredProfit(OrderBookResponse orderBook)
        {
            throw new NotImplementedException();
        }

        private decimal GetBuyPrice(OrderBookResponse orderBook, int assetPrecision, decimal quoteAssetPriceInDollars)
        {
            decimal bidValueToConsiderWall = SettingsService.GetBidValueToConsiderWall();
            decimal buyPrice = 0;

            foreach (var bid in orderBook.Bids.GetRange(0, 6))
            {
                decimal totalValue = Math.Round(bid.Price * bid.Quantity * quoteAssetPriceInDollars, 2);

                if (totalValue >= bidValueToConsiderWall) //Buy Wall Identified
                    return Math.Round(bid.Price + (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);
            }

            return buyPrice;
        }

        private decimal GetSellPrice(OrderBookResponse orderBook, decimal buyPrice, int assetPrecision, decimal quoteAssetPriceInDollars)
        {
            decimal askValueToConsiderWall = SettingsService.GetAskValueToConsiderWall();
            decimal sellPrice = 0;
            decimal currentPrice = 0;
            decimal desiredProfitPercentage = SettingsService.GetDesiredProfitPercentage();
            bool limitToProfit = SettingsService.GetLimitToProfit();

            foreach (var ask in orderBook.Asks.GetRange(0, 6))
            {
                decimal totalValue = Math.Round(ask.Price * ask.Quantity * quoteAssetPriceInDollars, 2);
                currentPrice = ask.Price;

                if (totalValue >= askValueToConsiderWall) //Sell Wall Identified
                {
                    sellPrice = ask.Price - (1 / (decimal)Math.Pow(10, assetPrecision));
                    break;
                }
            }

            if (sellPrice == 0)
                sellPrice = currentPrice;

            if ((sellPrice * 100 / buyPrice) - 100 > desiredProfitPercentage)
            {
                if (limitToProfit)
                    sellPrice = buyPrice + (buyPrice * 1 / 100);
            }
            else
                sellPrice = 0;

            return Math.Round(sellPrice, assetPrecision);
        }

        public int GetAssetPrecision(OrderBookResponse orderBook, int defaultAssetPrecision)
        {
            bool precisionFound = false;
            string zerosToFind = "";
            int zerosFound = 0;

            while (!precisionFound)
            {
                zerosToFind += "0";

                for (var index = 0; index < orderBook.Bids.Count; index++)
                {
                    decimal price = orderBook.Bids[index].Price;
                    precisionFound = price.ToString().Substring(price.ToString().Length - zerosToFind.Length, zerosToFind.Length) != zerosToFind;

                    if (precisionFound)
                        break;
                }

                if (!precisionFound)
                    zerosFound++;
            }

            return defaultAssetPrecision - zerosFound;
        }

        internal decimal GetOrderQuantity(decimal priceInUsd)
        {
            if (priceInUsd == 0)
                priceInUsd = (decimal)0.01;

            decimal dollarsToInvest = 50;
            return dollarsToInvest / priceInUsd;
        }
    }
}
