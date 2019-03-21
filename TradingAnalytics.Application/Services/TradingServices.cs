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
        public TradeOpportunityDTO ValidateTradeOpportunity(OrderBookResponse orderBook, decimal lastBaseAssetPrice, decimal baseAssetPriceInDollars, decimal quoteAssetPriceInDollars, int assetPrecision, string baseAsset, string quoteAsset, decimal minQty, decimal maxQty)
        {
            decimal buyPrice = GetBuyPrice(orderBook, assetPrecision, quoteAssetPriceInDollars);

            if (buyPrice == 0)
                return null;

            decimal sellPrice = GetSellPrice(orderBook, buyPrice, assetPrecision, quoteAssetPriceInDollars, false);

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
                    QuoteAssetPriceInUsd = quoteAssetPriceInDollars,
                    BaseAssetPrecision = assetPrecision,
                    OrderBook = orderBook,
                    MinQty = minQty,
                    MaxQty = maxQty
                };
            }
            else
                return null;
        }

        public int UpdateOrderStatus(string clientOrderId, string side, string status, decimal lastPrice, decimal quoteAssetPriceInDollars)
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.UpdateOrderStatus(clientOrderId, side, status, lastPrice, quoteAssetPriceInDollars);
        }

        public int UpdateLastPrice(string clientOrderId, string side, decimal lastPrice)
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.UpdateLastPrice(clientOrderId, side, lastPrice);
        }

        public bool LowerWallStillExists(OrderBookResponse orderBook, int assetPrecision, decimal quoteAssetPriceInDollars,  decimal buyPrice)
        {
            decimal bidValueToConsiderWall = SettingsService.GetBidValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();
            decimal wallPrice = Math.Round(buyPrice - (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);

            bool lowerWallStillExists = false;

            foreach (var bid in orderBook.Bids.GetRange(0, rangeToFind))
            {
                if (bid.Price == wallPrice)
                {
                    decimal totalValue = Math.Round(bid.Price * bid.Quantity * quoteAssetPriceInDollars, 2);
                    return (totalValue >= bidValueToConsiderWall);
                }
            }

            return lowerWallStillExists;
        }

        public List<Order> GetOpenOrders()
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.GetOpenOrders();
        }

        public bool UpperWallFormedBeforeDesiredProfit(OrderBookResponse orderBook, decimal quoteAssetPriceInDollars, decimal buyPrice, decimal sellPrice, out decimal wallPrice)
        {
            decimal askValueToConsiderWall = SettingsService.GetAskValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();

            wallPrice = 0;

            foreach (var ask in orderBook.Asks.GetRange(0, rangeToFind))
            {
                decimal totalValue = Math.Round(ask.Price * ask.Quantity * quoteAssetPriceInDollars, 2);

                if (totalValue >= askValueToConsiderWall && ask.Price > buyPrice) //Sell Wall Identified
                    if (ask.Price < sellPrice)
                        if (wallPrice == 0 || ask.Price < wallPrice)
                            wallPrice = ask.Price;
            }

            return wallPrice != 0;
        }

        public int UpdateSellOrderUrderWall(string clientOrderId, int assetPrecision, decimal sellWallPrice)
        {
            OrderRepository orderRepository = new OrderRepository();
            decimal sellPrice = Math.Round(sellWallPrice - (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);

            return orderRepository.UpdateSellOrderPrice(clientOrderId, sellPrice);
        }

        public decimal GetBuyPrice(OrderBookResponse orderBook, int assetPrecision, decimal quoteAssetPriceInDollars)
        {
            decimal bidValueToConsiderWall = SettingsService.GetBidValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();
            decimal buyPrice = 0;

            foreach (var bid in orderBook.Bids.GetRange(0, rangeToFind))
            {
                decimal totalValue = Math.Round(bid.Price * bid.Quantity * quoteAssetPriceInDollars, 2);

                if (totalValue >= bidValueToConsiderWall) //Buy Wall Identified
                    return Math.Round(bid.Price + (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);
            }

            return buyPrice;
        }

        public decimal GetSellPrice(OrderBookResponse orderBook, decimal buyPrice, int assetPrecision, decimal quoteAssetPriceInDollars, bool adjustingSellPrice)
        {
            decimal askValueToConsiderWall = SettingsService.GetAskValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();
            decimal sellPrice = 0;
            decimal currentPrice = 0;
            decimal desiredProfitPercentage = SettingsService.GetDesiredProfitPercentage();
            bool limitToProfit = SettingsService.GetLimitToProfit();

            foreach (var ask in orderBook.Asks.GetRange(0, rangeToFind))
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
                sellPrice = buyPrice + (buyPrice * desiredProfitPercentage / 100);

            if ((sellPrice * 100 / buyPrice) - 100 >= desiredProfitPercentage)
            {
                if (limitToProfit)
                    sellPrice = buyPrice + (buyPrice * desiredProfitPercentage / 100);
            }
            else if (!adjustingSellPrice)
                sellPrice = 0;

            return Math.Round(sellPrice, assetPrecision);
        }

        public int UpdateSellOrderPrice(string clientOrderId, decimal sellPrice)
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.UpdateSellOrderPrice(clientOrderId, sellPrice);
        }

        public int GetAssetPrecision(OrderBookResponse orderBook)
        {
            bool precisionFound = false;
            string zerosToFind = "";
            int zerosFound = 0;
            int defaultAssetPrecision = 0;

            while (!precisionFound)
            {
                zerosToFind += "0";

                for (var index = 0; index < orderBook.Bids.Count; index++)
                {
                    decimal price = orderBook.Bids[index].Price;

                    if (defaultAssetPrecision == 0)
                    {
                        int dotIndex = price.ToString().LastIndexOf(",");

                        if (dotIndex != -1)
                            defaultAssetPrecision = price.ToString().Substring(dotIndex).Length - 1;
                    }

                    precisionFound = price.ToString().Substring(price.ToString().Length - zerosToFind.Length, zerosToFind.Length) != zerosToFind;

                    if (precisionFound)
                        break;
                }

                if (!precisionFound)
                    zerosFound++;
            }

            return defaultAssetPrecision - zerosFound;
        }

        internal decimal GetOrderQuantity(decimal priceInUsd, decimal minQty, decimal maxQty)
        {
            if (priceInUsd == 0)
                priceInUsd = (decimal)0.01;

            int qtyAssetPrecision = GetValueAssetPrecision(minQty);

            decimal dollarsToInvest = SettingsService.GetDollarsToInvest();
            decimal qty = Math.Round(dollarsToInvest / priceInUsd, qtyAssetPrecision);

            if (qty < minQty)
                qty = minQty;

            if (qty > maxQty)
                qty = maxQty;

            return qty;
        }

        public int GetValueAssetPrecision(decimal minQty)
        {
            bool precisionFound = false;
            string zerosToFind = "";
            int zerosFound = 0;
            int defaultAssetPrecision = 0;

            while (!precisionFound)
            {
                zerosToFind += "0";

                decimal price = minQty;

                if (defaultAssetPrecision == 0)
                {
                    int dotIndex = price.ToString().LastIndexOf(",");

                    if (dotIndex != -1)
                        defaultAssetPrecision = price.ToString().Substring(dotIndex).Length - 1;
                }

                precisionFound = price.ToString().Substring(price.ToString().Length - zerosToFind.Length, zerosToFind.Length) != zerosToFind;

                if (!precisionFound)
                    zerosFound++;
            }

            return defaultAssetPrecision - zerosFound;
        }
    }
}
