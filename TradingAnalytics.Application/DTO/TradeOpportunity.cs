using BinanceExchange.API.Models.Response;

namespace TradingAnalytics.Application.DTO
{
    public class TradeOpportunityDTO
    {
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal LastBaseAssetPrice { get; set; }
        public decimal BaseAssetPriceInUsd { get; set; }
        public decimal QuoteAssetPriceInUsd { get; set; }
        public int BaseAssetPrecision { get; set; }
        public OrderBookResponse OrderBook { get; set; }
        public decimal MinQty { get; set; }
        public decimal MaxQty { get; set; }
    }
}