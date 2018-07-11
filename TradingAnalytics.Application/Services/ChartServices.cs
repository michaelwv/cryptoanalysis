using BinanceExchange.API.Models.Response;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using TradingAnalytics.Application.DTO;

namespace TradingAnalytics.Application.Services
{
    public class ChartServices
    {
        public FileParameter GenerateOrderBookChartImage(decimal quoteAssetPriceInDollars, TradeOpportunityDTO tradeOpportunity)
        {
            string html = "<html>" +
                        "   <head>" +
                        "       <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />" +
                        getStyle() +
                        "   </head>" +
                        "   <body>" +
                        "       <div style='font-size: 18px;'>" + tradeOpportunity.BaseAsset + "-" + tradeOpportunity.QuoteAsset + " ($" + tradeOpportunity.BaseAssetPriceInUsd.ToString("0.00") + ")</div>" +
                        "       <div style='font-size: 16px;'>BUY: " + tradeOpportunity.BuyPrice.ToString() + " - SELL: " + tradeOpportunity.SellPrice.ToString() + "</div>" +
                        "       <div class='container'>" + GetBidsAsksHtml(tradeOpportunity.OrderBook.Asks, "sell", quoteAssetPriceInDollars, tradeOpportunity.BaseAssetPrecision) + "<div class='separador'>&nbsp;</div>" + GetBidsAsksHtml(tradeOpportunity.OrderBook.Bids, "buy", quoteAssetPriceInDollars, tradeOpportunity.BaseAssetPrecision) + "</div>" +
                        "       </div>" +
                        "   </body>" +
                        "</html>";

            var htmlToImageConv = new NReco.ImageGenerator.HtmlToImageConverter();

            htmlToImageConv.Width = (int)(SettingsService.GetMaxChartWidthInPixels() + 20);

            var imageBytes = htmlToImageConv.GenerateImage(html, ImageFormat.Jpeg.ToString());

            string fileName = String.Format("{0}.jpg", tradeOpportunity.BaseAsset + "-" + tradeOpportunity.QuoteAsset);

            MemoryStream memoryStream = new MemoryStream(imageBytes);

            FileParameter file = new FileParameter(memoryStream.ToArray(), fileName, "image/jpeg");

            memoryStream.Close();

            return file;
        }

        private static string getStyle()
        {
            return "<style>" +
                   "    .row {" +
                   "        display: -webkit-box;" +
                   "        display: -ms-flexbox;" +
                   "        display: flex;" +
                   "        -ms-flex-wrap: wrap;" +
                   "        flex-wrap: wrap;" +
                   "        height: 22px;" +
                   "    }" +
                   "    .col {" +
                   "        -webkit-box-flex: 1;" +
                   "        - ms-flex: 1 0 0px;" +
                   "        flex: 1 0 0;" +
                   "        max-width: 100%;" +
                   "        background-color: transparent;" +
                   "        z-index: 9999;" +
                   "        margin: 1px 0px 1px 0px;" +
                   "        font-weight: bolder;" +
                   "        padding: 2px;" +
                   "        font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif;" +
                   "        position: relative;" +
                   "    }" +
                   "    .container {" +
                   "        width: 350px;" +
                   "        background-color: #4c4c4c;" +
                   "        height: 451px;" +
                   "        display: -webkit-box;" +
                   "        display: -ms-flexbox;" +
                   "        display: flex;" +
                   "        -webkit-box-orient: vertical;" +
                   "        -webkit-box-direction: normal;" +
                   "        -ms-flex-direction: column;" +
                   "        flex-direction: column;" +
                   "        position: relative;" +
                   "        margin-bottom: 1rem;" +
                   "    }" +
                   "    .row.buy .lineValue {" +
                   "        background-color: #43ad40;" +
                   "    }" +
                   "    .row.buy .col {" +
                   "        color: white;" +
                   "        font-size: 13px;" +
                   "    }" +
                   "    .row.sell .col {" +
                   "        color: white;" +
                   "        font-size: 13px;" +
                   "    }" +
                   "    .row.sell .lineValue {" +
                   "        background-color: #990707;" +
                   "    }" +
                   "    div.lineValue {" +
                   "        position: absolute;" +
                   "        z-index: 1;" +
                   "    }" +
                   "    .separador {" +
                   "        height: 1px;" +
                   "        width: 100%;" +
                   "        background-color: #a4a5a4;" +
                   "        margin: 5px 0px 5px 0px;" +
                   "    }" +
                   "</style>";
        }

        private static string GetBidsAsksHtml(List<TradeResponse> values, string operationType, decimal quoteAssetPriceInDollars, int assetPrecision)
        {
            string html = "";
            decimal maxWidth = SettingsService.GetMaxChartWidthInPixels();
            decimal maxWallWidthInUSD = SettingsService.GetMaxWallWidthInUSD();

            if (operationType == "sell")
                values.Reverse();

            foreach (var value in values)
            {
                decimal width = 0;
                decimal totalValue = Math.Round(value.Price * value.Quantity * quoteAssetPriceInDollars, assetPrecision);

                if (totalValue >= maxWallWidthInUSD)
                    width = maxWidth;
                else
                    width = Math.Round(totalValue * maxWidth / maxWallWidthInUSD, 0);

                html += String.Format(
                        "<div class='row {0}'>" +
                        "   <div class='lineValue' style='width: {1}px'>&nbsp;</div>" +
                        "   <div class='col'>{2}</div>" +
                        "   <div class='col'>{3}</div>" +
                        "   <div class='col'>{4}</div>" +
                        "</div>"
                    , operationType, width, Math.Round(value.Price, assetPrecision), Math.Round(value.Quantity, 2), Math.Round(totalValue, 8));
            }

            return html;
        }
    }
}
