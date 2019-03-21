using System;
using System.Collections.Generic;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.DataAccess
{
    public class OrderRepository
    {
        public bool PendingOrderExists(string baseAsset, string quoteAsset, string[] pendingStatus)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "SELECT COUNT(*) FROM orders " +
                             "WHERE BaseAsset = @baseAsset " +
                             "AND QuoteAsset = @quoteAsset " +
                             "AND (";

                for (int i = 0; i < pendingStatus.Length; i++)
                {
                    if (i > 0)
                        cmd += " OR ";

                    cmd += "BuyStatus = '" + pendingStatus[i] + "' OR SellStatus = '" + pendingStatus[i] + "'";
                }

                cmd += ")";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@baseAsset", baseAsset },
                    { "@quoteAsset", quoteAsset }
                };

                var result = mySqlDataAccess.ExecuteScalar(cmd, arrParam);

                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int CreateOrder(Order order)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "INSERT INTO orders (BaseAsset, QuoteAsset, AssetPrecision, Quantity, BuyPrice, BuyStatus, BuyStatusDate, BuyClientOrderId, BuyIncDate, SellPrice) VALUES (@baseAsset, @quoteAsset, @assetPrecision, @quantity, @buyPrice, @buyStatus, @buyStatusDate, @buyClientOrderId, @buyIncDate, @sellPrice)";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@baseAsset", order.BaseAsset },
                    { "@quoteAsset", order.QuoteAsset },
                    { "@assetPrecision", order.AssetPrecision },
                    { "@quantity", order.Quantity },
                    { "@buyPrice", order.BuyPrice },
                    { "@buyStatus", order.BuyStatus },
                    { "@buyStatusDate", DateTime.Now },
                    { "@buyClientOrderId", order.BuyClientOrderId },
                    { "@buyIncDate", order.BuyIncDate },
                    { "@sellPrice", order.SellPrice }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateOrderStatus(string clientOrderId, string side, string status, decimal lastPrice, decimal quoteAssetPriceInDollars)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             (side.ToUpper() == "BUY" ? " BuyStatus" : " SellStatus") + " = @status" +
                             ", " + (side.ToUpper() == "BUY" ? " BuyStatusDate" : " SellStatusDate") + " = @statusDate" +
                             ", LastPrice = @lastPrice" +
                             ", LastPriceDate = @lastPriceDate";
#if DEBUG
                if (side.ToUpper() == "BUY" && status == "FILLED")
                {
                    cmd += ", SellStatus = 'NEW', SellStatusDate = @sellStatusDate, SellIncDate = @sellIncDate";
                }
#endif

                if (status == "FILLED")
                {
                    cmd += ", " + (side.ToUpper() == "BUY" ? " QuoteAssetPriceAtBuy" : " QuoteAssetPriceAtSell") + " = @quoteAssetPriceInDollars";
                }

                cmd += " WHERE " + (side.ToUpper() == "BUY" ? " BuyClientOrderId" : " SellClientOrderId") + " = @clientOrderId;";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@status", status },
                    { "@statusDate", DateTime.Now },
                    { "@lastPrice", lastPrice },
                    { "@lastPriceDate", DateTime.Now },
                    { "@clientOrderId", clientOrderId },
                    { "@sellStatusDate", DateTime.Now },
                    { "@sellIncDate", DateTime.Now },
                    { "@quoteAssetPriceInDollars", quoteAssetPriceInDollars }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateLastPrice(string clientOrderId, string side, decimal lastPrice)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             "  LastPrice = @lastPrice" +
                             ", LastPriceDate = @lastPriceDate" +
                             " WHERE " + (side.ToUpper() == "BUY" ? " BuyClientOrderId" : " SellClientOrderId") + " = @clientOrderId;";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@lastPrice", lastPrice },
                    { "@lastPriceDate", DateTime.Now },
                    { "@clientOrderId", clientOrderId }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateSellOrderPrice(string clientOrderId, decimal sellPrice)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             "  SellPrice = @sellPrice" +
                             " WHERE SellClientOrderId = @clientOrderId;";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@sellPrice", sellPrice },
                    { "@clientOrderId", clientOrderId }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Order> GetOpenOrders()
        {
            MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();

            try
            {
                List<Order> orders = new List<Order>();
                string cmd = " SELECT * FROM orders WHERE (BuyStatus = 'NEW' OR SellStatus = 'NEW')";

                var result = mySqlDataAccess.ExecuteReader(cmd, null);

                while (result.Read())
                {
                    var order = new Order();

                    order.BaseAsset = result.GetString("BaseAsset");
                    order.QuoteAsset = result.GetString("QuoteAsset");
                    order.AssetPrecision = result.GetInt16("AssetPrecision");
                    order.Quantity = result.GetDecimal("Quantity");
                    order.BuyPrice = result.GetDecimal("BuyPrice");
                    order.BuyStatus = result.GetString("BuyStatus");
                    order.BuyClientOrderId = (result.IsDBNull(result.GetOrdinal("BuyClientOrderId"))) ? null : result.GetString("BuyClientOrderId");
                    order.BuyIncDate = result.GetDateTime("BuyIncDate");

                    if (!result.IsDBNull(result.GetOrdinal("QuoteAssetPriceAtBuy")))
                        order.QuoteAssetPriceAtBuy = result.GetDecimal("QuoteAssetPriceAtBuy");
                    else
                        order.QuoteAssetPriceAtBuy = null;

                    order.SellPrice = result.GetDecimal("SellPrice");
                    order.SellStatus = (result.IsDBNull(result.GetOrdinal("SellStatus"))) ? null : result.GetString("SellStatus");
                    order.SellClientOrderId = (result.IsDBNull(result.GetOrdinal("SellClientOrderId"))) ? null : result.GetString("SellClientOrderId");

                    if (!result.IsDBNull(result.GetOrdinal("SellIncDate")))
                        order.SellIncDate = result.GetDateTime("SellIncDate");
                    else
                        order.SellIncDate = null;

                    if (!result.IsDBNull(result.GetOrdinal("QuoteAssetPriceAtSell")))
                        order.QuoteAssetPriceAtSell = result.GetDecimal("QuoteAssetPriceAtSell");
                    else
                        order.QuoteAssetPriceAtSell = null;

                    if (!result.IsDBNull(result.GetOrdinal("LastPrice")))
                        order.LastPrice = result.GetDecimal("LastPrice");
                    else
                        order.LastPrice = null;

                    if (!result.IsDBNull(result.GetOrdinal("LastPriceDate")))
                        order.LastPriceDate = result.GetDateTime("LastPriceDate");
                    else
                        order.LastPriceDate = null;

                    orders.Add(order);
                }

                mySqlDataAccess.CloseConnection();

                return orders;
            }
            catch (Exception ex)
            {
                mySqlDataAccess.CloseConnection();

                throw ex;
            }
        }

        public void UpdateClientOrderId()
        {
            MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();

            string cmd = " UPDATE orders SET BuyClientOrderId = Id, SellClientOrderId = Id WHERE BuyClientOrderId IS NULL OR SellClientOrderId IS NULL";

            var result = mySqlDataAccess.ExecuteNonQuery(cmd, null);
        }
    }
}
