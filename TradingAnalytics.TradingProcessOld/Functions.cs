using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Response;
using Microsoft.Azure.WebJobs;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.Application.Services;

namespace TradingAnalytics.TradingProcessOld
{
    public class Functions
    {
        public async System.Threading.Tasks.Task ProcessMethodAsync([TimerTrigger("00:05:00", RunOnStartup = true)] TimerInfo timerInfo, TextWriter log)
        {
            Console.WriteLine("----------------Process Started-------------------");

            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();

            List<ExchangeInfoSymbol> tradingCoins = await binanceService.GetTradingCoins();

            List<TradeOpportunityDTO> tradeOpportunitiesFirstCheck = new List<TradeOpportunityDTO>();
            List<TradeOpportunityDTO> tradeOpportunitiesSecondCheck = new List<TradeOpportunityDTO>();
            List<TradeOpportunityDTO> tradeOpportunitiesThirdCheck = new List<TradeOpportunityDTO>();
            List<TradeOpportunityDTO> tradeOpportunitiesFourthCheck = new List<TradeOpportunityDTO>();

            decimal quoteAssetPriceInDollars = Math.Round(await binanceService.GetCurrentPrice(SettingsService.GetQuoteAssetToTrade() + "USDT"), 2);

            foreach (ExchangeInfoSymbol coin in tradingCoins)
            {
                var tradeOpportunityFirstCheck = GetTradeOpportunity(coin.BaseAssetPrecision, coin.BaseAsset, coin.QuoteAsset, quoteAssetPriceInDollars);

                if (tradeOpportunityFirstCheck != null)
                {
                    tradeOpportunitiesFirstCheck.Add(tradeOpportunityFirstCheck);
                    Console.WriteLine("First Check: " + tradeOpportunityFirstCheck.BaseAsset + tradeOpportunityFirstCheck.QuoteAsset);
                }
            }

            Console.WriteLine("------------First Check Finished------------------");

            System.Threading.Thread.Sleep(60000);

            foreach (TradeOpportunityDTO tradeOpportunityFirstCheck in tradeOpportunitiesFirstCheck)
            {
                var tradeOpportunitySecondCheck = GetTradeOpportunity(tradeOpportunityFirstCheck.BaseAssetPrecision, tradeOpportunityFirstCheck.BaseAsset, tradeOpportunityFirstCheck.QuoteAsset, quoteAssetPriceInDollars);

                if (tradeOpportunitySecondCheck != null)
                {
                    tradeOpportunitiesSecondCheck.Add(tradeOpportunitySecondCheck);
                    Console.WriteLine("Second Check: " + tradeOpportunitySecondCheck.BaseAsset + tradeOpportunitySecondCheck.QuoteAsset);
                }
            }

            Console.WriteLine("------------Second Check Finished-----------------");

            System.Threading.Thread.Sleep(60000);

            foreach (TradeOpportunityDTO tradeOpportunitySecondCheck in tradeOpportunitiesSecondCheck)
            {
                var tradeOpportunityThirdCheck = GetTradeOpportunity(tradeOpportunitySecondCheck.BaseAssetPrecision, tradeOpportunitySecondCheck.BaseAsset, tradeOpportunitySecondCheck.QuoteAsset, quoteAssetPriceInDollars);

                if (tradeOpportunityThirdCheck != null)
                {
                    tradeOpportunitiesThirdCheck.Add(tradeOpportunityThirdCheck);
                    Console.WriteLine("Third Check: " + tradeOpportunityThirdCheck.BaseAsset + tradeOpportunityThirdCheck.QuoteAsset);
                }
            }

            Console.WriteLine("-------------Third Check Finished-----------------");

            System.Threading.Thread.Sleep(60000);

            foreach (TradeOpportunityDTO tradeOpportunityThirdCheck in tradeOpportunitiesThirdCheck)
            {
                var tradeOpportunityFourthCheck = GetTradeOpportunity(tradeOpportunityThirdCheck.BaseAssetPrecision, tradeOpportunityThirdCheck.BaseAsset, tradeOpportunityThirdCheck.QuoteAsset, quoteAssetPriceInDollars);

                if (tradeOpportunityFourthCheck != null)
                {
                    tradeOpportunitiesFourthCheck.Add(tradeOpportunityFourthCheck);
                    Console.WriteLine("Fourth Check: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset);
                }
            }

            Console.WriteLine("-------------Fourth Check Finished----------------");

            foreach (TradeOpportunityDTO tradeOpportunityFourthCheck in tradeOpportunitiesFourthCheck)
            {
                bool orderSet = binanceService.SetNewOrder(tradeOpportunityFourthCheck, OrderSide.Buy, OrderType.Limit);
            
                if (orderSet)
                {
                    Console.WriteLine("New Order: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset + " - Buy Price: " + tradeOpportunityFourthCheck.BuyPrice + " - Sell Price: " + tradeOpportunityFourthCheck.SellPrice);

                    ChartServices chartServices = new ChartServices();

                    FileParameter chartImage = chartServices.GenerateOrderBookChartImage(quoteAssetPriceInDollars, tradeOpportunityFourthCheck);
            
                    TelegramService telegramService = new TelegramService();
                    HttpResponseMessage response = await telegramService.SendImageAsync(chartImage);
            
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception(response.ReasonPhrase);
                }
            }

            Console.WriteLine("-----------------Process Finished-----------------");
            Console.Clear();
        }

        private TradeOpportunityDTO GetTradeOpportunity(int defaultAssetPrecision, string baseAsset, string quoteAsset, decimal quoteAssetPriceInDollars)
        {
            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();

            OrderBookResponse orderBook = binanceService.GetOrderBook(baseAsset + quoteAsset).Result;

            decimal lastBaseAssetPrice = binanceService.GetCurrentPrice(baseAsset + quoteAsset).Result;

            decimal baseAssetPriceInDollars = Math.Round(lastBaseAssetPrice * quoteAssetPriceInDollars, 2);

            int baseAssetPrecision = tradingServices.GetAssetPrecision(orderBook, defaultAssetPrecision);

            return tradingServices.ValidateTradeOpportunity(orderBook, lastBaseAssetPrice, baseAssetPriceInDollars, quoteAssetPriceInDollars, baseAssetPrecision, baseAsset, quoteAsset);
        }
    }
}
