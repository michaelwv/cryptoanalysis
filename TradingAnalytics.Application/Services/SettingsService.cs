using System;
using TradingAnalytics.Application.DTO;

namespace TradingAnalytics.Application.Services
{
    public static class SettingsService
    {
        public static SecurityDTO GetBinanceKeys()
        {
            return new SecurityDTO
            {
                ApiKey = Properties.Settings.Default.BinanceApiKey,
                SecretKey = Properties.Settings.Default.BinanceSecretKey
            };
        }

        public static string GetQuoteAssetToTrade()
        {
            return Properties.Settings.Default.QuoteAssetToTrade;
        }

        public static string GetBinanceEndPoint()
        {
            return Properties.Settings.Default.BinanceEndPoint;
        }

        public static decimal GetBidValueToConsiderWall()
        {
            return Properties.Settings.Default.BidValueToConsiderWall;
        }

        public static decimal GetAskValueToConsiderWall()
        {
            return Properties.Settings.Default.AskValueToConsiderWall;
        }

        public static decimal GetMaxChartWidthInPixels()
        {
            return Properties.Settings.Default.MaxChartWidthInPixels;
        }

        public static decimal GetMaxWallWidthInUSD()
        {
            return Properties.Settings.Default.MaxWallWidthInUSD;
        }

        public static decimal GetDesiredProfitPercentage()
        {
            return Properties.Settings.Default.DesiredProfitPercentage;
        }

        public static bool GetLimitToProfit()
        {
            return Properties.Settings.Default.LimitToProfit;
        }

        public static int GetRangeToFind()
        {
            return Properties.Settings.Default.RangeToFind;
        }

        public static decimal GetDollarsToInvest()
        {
            return Properties.Settings.Default.DollarsToInvest;
        }

        public static decimal GetCoinVolumeToConsider()
        {
            return Properties.Settings.Default.CoinVolumeToConsider;
        }
    }
}
