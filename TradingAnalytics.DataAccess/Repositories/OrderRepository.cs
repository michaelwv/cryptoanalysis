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
                string cmd = "INSERT INTO orders (BaseAsset, QuoteAsset, Quantity, BuyPrice, BuyStatus, BuyClientOrderId, BuyIncDate, SellPrice) VALUES (@baseAsset, @quoteAsset, @quantity, @buyPrice, @buyStatus, @buyClientOrderId, @buyIncDate, @sellPrice)";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@baseAsset", order.BaseAsset },
                    { "@quoteAsset", order.QuoteAsset },
                    { "@quantity", order.Quantity },
                    { "@buyPrice", order.BuyPrice },
                    { "@buyStatus", order.BuyStatus },
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

        public int UpdateOrderStatus(string clientOrderId, string side, string status, decimal lastPrice)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             (side.ToUpper() == "BUY" ? " BuyStatus" : " SellStatus") + " = @status" +
                             ", LastPrice = @lastPrice" +
                             ", LastPriceDate = @lastPriceDate" +
                             " WHERE " + (side.ToUpper() == "BUY" ? " BuyClientOrderId" : " SellClientOrderId") + " = @clientOrderId;";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@status", status },
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

        public List<Order> GetOpenOrders()
        {
            MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();

            try
            {
                List<Order> orders = new List<Order>();
                string cmd = " SELECT * FROM orders WHERE (BuyStatus NOT IN('FILLED', 'CANCELED') OR SellStatus NOT IN('FILLED', 'CANCELED'))";

                var result = mySqlDataAccess.ExecuteReader(cmd, null);

                while (result.Read())
                {
                    orders.Add(new Order()
                    {
                        BaseAsset = result.GetString("BaseAsset"),
                        QuoteAsset = result.GetString("QuoteAsset"),
                        Quantity = result.GetDecimal("Quantity"),
                        BuyPrice = result.GetDecimal("BuyPrice"),
                        BuyStatus = result.GetString("BuyStatus"),
                        BuyClientOrderId = (result.IsDBNull(result.GetOrdinal("BuyClientOrderId"))) ? null : result.GetString("BuyClientOrderId"),
                        BuyIncDate = result.GetDateTime("BuyIncDate"),
                        SellPrice = result.GetDecimal("SellPrice"),
                        SellStatus = result.GetString("SellStatus"),
                        SellClientOrderId = (result.IsDBNull(result.GetOrdinal("SellClientOrderId"))) ? null : result.GetString("SellClientOrderId"),
                        SellIncDate = result.GetDateTime("SellIncDate"),
                        LastPrice = result.GetDecimal("LastPrice"),
                        LastPriceDate = result.GetDateTime("LastPriceDate")
                    });
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
    }
}
