using System;
using log4net;
using System.Collections.Generic;
using System.Net.Http;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.Application.Services;
using Newtonsoft.Json;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Enums;

namespace TradingAnalytics.TradingProcess
{
    public class Functions
    {
        public void ProcessTrades()
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                logger.Debug("----------------Process Started-------------------");
                Console.WriteLine("----------------Process Started-------------------");

                BinanceService binanceService = new BinanceService();
                TradingServices tradingServices = new TradingServices();

                List<ExchangeInfoSymbol> tradingCoins = binanceService.GetTradingCoins().Result;

                List<TradeOpportunityDTO> tradeOpportunitiesFirstCheck = new List<TradeOpportunityDTO>();
                List<TradeOpportunityDTO> tradeOpportunitiesSecondCheck = new List<TradeOpportunityDTO>();
                List<TradeOpportunityDTO> tradeOpportunitiesThirdCheck = new List<TradeOpportunityDTO>();
                List<TradeOpportunityDTO> tradeOpportunitiesFourthCheck = new List<TradeOpportunityDTO>();

                decimal quoteAssetPriceInDollars = Math.Round(binanceService.GetCurrentPrice(SettingsService.GetQuoteAssetToTrade() + "USDT").Result, 2);
                decimal coinVolumeToConsider = SettingsService.GetCoinVolumeToConsider();
                decimal minQty = 0;
                decimal maxQty = 0;

                foreach (ExchangeInfoSymbol coin in tradingCoins)
                {
                    var volume = GetCoinDailyVolume(coin.BaseAsset + coin.QuoteAsset);

                    if (volume < coinVolumeToConsider)
                    {
                        //logger.Debug(coin.BaseAsset + coin.QuoteAsset + ": Insufficient daily volume.");
                        //Console.WriteLine(coin.BaseAsset + coin.QuoteAsset + ": Insufficient daily volume.");
                        continue;
                    }

                    var filter = coin.Filters.Find(x => x.FilterType.ToString() == "LotSize");

                    if (filter != null)
                    {
                        ExchangeInfoSymbolFilterLotSize filterLotSize = (ExchangeInfoSymbolFilterLotSize)filter;
                        minQty = filterLotSize.MinQty;
                        maxQty = filterLotSize.MaxQty;
                    }

                    var tradeOpportunityFirstCheck = GetTradeOpportunity(coin.BaseAsset, coin.QuoteAsset, quoteAssetPriceInDollars, minQty, maxQty);

                    if (tradeOpportunityFirstCheck != null)
                    {
                        tradeOpportunitiesFirstCheck.Add(tradeOpportunityFirstCheck);
                        logger.Debug("First Check: " + tradeOpportunityFirstCheck.BaseAsset + tradeOpportunityFirstCheck.QuoteAsset);
                        Console.WriteLine("First Check: " + tradeOpportunityFirstCheck.BaseAsset + tradeOpportunityFirstCheck.QuoteAsset);
                    }
                }

                logger.Debug("------------First Check Finished------------------");
                Console.WriteLine("------------First Check Finished------------------");

                System.Threading.Thread.Sleep(60000);

                foreach (TradeOpportunityDTO tradeOpportunityFirstCheck in tradeOpportunitiesFirstCheck)
                {
                    var tradeOpportunitySecondCheck = GetTradeOpportunity(tradeOpportunityFirstCheck.BaseAsset, tradeOpportunityFirstCheck.QuoteAsset, quoteAssetPriceInDollars, tradeOpportunityFirstCheck.MinQty, tradeOpportunityFirstCheck.MaxQty);

                    if (tradeOpportunitySecondCheck != null)
                    {
                        tradeOpportunitiesSecondCheck.Add(tradeOpportunitySecondCheck);
                        logger.Debug("Second Check: " + tradeOpportunitySecondCheck.BaseAsset + tradeOpportunitySecondCheck.QuoteAsset);
                        Console.WriteLine("Second Check: " + tradeOpportunitySecondCheck.BaseAsset + tradeOpportunitySecondCheck.QuoteAsset);
                    }
                }

                logger.Debug("------------Second Check Finished-----------------");
                Console.WriteLine("------------Second Check Finished-----------------");

                System.Threading.Thread.Sleep(60000);

                foreach (TradeOpportunityDTO tradeOpportunitySecondCheck in tradeOpportunitiesSecondCheck)
                {
                    var tradeOpportunityThirdCheck = GetTradeOpportunity(tradeOpportunitySecondCheck.BaseAsset, tradeOpportunitySecondCheck.QuoteAsset, quoteAssetPriceInDollars, tradeOpportunitySecondCheck.MinQty, tradeOpportunitySecondCheck.MaxQty);

                    if (tradeOpportunityThirdCheck != null)
                    {
                        tradeOpportunitiesThirdCheck.Add(tradeOpportunityThirdCheck);
                        logger.Debug("Third Check: " + tradeOpportunityThirdCheck.BaseAsset + tradeOpportunityThirdCheck.QuoteAsset);
                        Console.WriteLine("Third Check: " + tradeOpportunityThirdCheck.BaseAsset + tradeOpportunityThirdCheck.QuoteAsset);
                    }
                }

                logger.Debug("-------------Third Check Finished-----------------");
                Console.WriteLine("-------------Third Check Finished-----------------");

                System.Threading.Thread.Sleep(60000);

                foreach (TradeOpportunityDTO tradeOpportunityThirdCheck in tradeOpportunitiesThirdCheck)
                {
                    var tradeOpportunityFourthCheck = GetTradeOpportunity(tradeOpportunityThirdCheck.BaseAsset, tradeOpportunityThirdCheck.QuoteAsset, quoteAssetPriceInDollars, tradeOpportunityThirdCheck.MinQty, tradeOpportunityThirdCheck.MaxQty);

                    if (tradeOpportunityFourthCheck != null)
                    {
                        tradeOpportunitiesFourthCheck.Add(tradeOpportunityFourthCheck);
                        logger.Debug("Fourth Check: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset);
                        Console.WriteLine("Fourth Check: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset);
                    }
                }

                logger.Debug("-------------Fourth Check Finished----------------");
                Console.WriteLine("-------------Fourth Check Finished----------------");

                foreach (TradeOpportunityDTO tradeOpportunityFourthCheck in tradeOpportunitiesFourthCheck)
                {
                    bool orderSet = binanceService.SetNewOrder(tradeOpportunityFourthCheck, OrderSide.Buy, OrderType.Limit);

                    if (orderSet)
                    {
                        logger.Debug("New Order: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset + " - Buy Price: " + tradeOpportunityFourthCheck.BuyPrice + " - Sell Price: " + tradeOpportunityFourthCheck.SellPrice);
                        Console.WriteLine("New Order: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset + " - Buy Price: " + tradeOpportunityFourthCheck.BuyPrice + " - Sell Price: " + tradeOpportunityFourthCheck.SellPrice);

                        ChartServices chartServices = new ChartServices();

                        FileParameter chartImage = chartServices.GenerateOrderBookChartImage(quoteAssetPriceInDollars, tradeOpportunityFourthCheck);

                        TelegramService telegramService = new TelegramService();
                        HttpResponseMessage response = telegramService.SendImageAsync(chartImage).Result;

                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            throw new Exception(response.ReasonPhrase);
                    }
                }

                logger.Debug("-----------------Process Finished-----------------");
                Console.WriteLine("-----------------Process Finished-----------------");
                Console.Clear();
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));

                TelegramService telegramService = new TelegramService();
                HttpResponseMessage response = telegramService.SendMessageAsync(JsonConvert.SerializeObject(e)).Result;
            }
        }

        private TradeOpportunityDTO GetTradeOpportunity(string baseAsset, string quoteAsset, decimal quoteAssetPriceInDollars, decimal minQty, decimal maxQty)
        {
            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();

            OrderBookResponse orderBook = binanceService.GetOrderBook(baseAsset + quoteAsset).Result;

            decimal lastBaseAssetPrice = binanceService.GetCurrentPrice(baseAsset + quoteAsset).Result;

            int baseAssetPrecision = tradingServices.GetAssetPrecision(orderBook);

            if (baseAssetPrecision <= 0 || baseAssetPrecision > 8)
                return null;

            decimal baseAssetPriceInDollars = Math.Round(lastBaseAssetPrice * quoteAssetPriceInDollars, baseAssetPrecision);

            return tradingServices.ValidateTradeOpportunity(orderBook, lastBaseAssetPrice, baseAssetPriceInDollars, quoteAssetPriceInDollars, baseAssetPrecision, baseAsset, quoteAsset, minQty, maxQty);
        }

        public decimal GetCoinDailyVolume(string symbol)
        {
            BinanceService binanceService = new BinanceService();
            decimal dailyVolume = 0;

            var dailyTicker = binanceService.GetDailyTicker(symbol).Result;

            if (dailyTicker != null)
                dailyVolume = dailyTicker.QuoteVolume;

            return dailyVolume;
        }
    }
}
