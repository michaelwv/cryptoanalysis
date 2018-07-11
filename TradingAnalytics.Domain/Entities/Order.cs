using System;

namespace TradingAnalytics.Domain.Entities
{
    public class Order
    {
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public decimal Quantity { get; set; }
        public decimal BuyPrice { get; set; }
        public string BuyStatus { get; set; }
        public string BuyClientOrderId { get; set; }
        public DateTime BuyIncDate { get; set; }
        public decimal SellPrice { get; set; }
        public string SellStatus { get; set; }
        public string SellClientOrderId { get; set; }
        public DateTime SellIncDate { get; set; }
        public decimal LastPrice { get; set; }
        public DateTime LastPriceDate { get; set; }
    }
}
