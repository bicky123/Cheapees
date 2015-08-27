using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cheapees.ChannelAdvisorOrderService;

namespace Cheapees
{
  public class ChannelAdvisorSalesDataViewModel : UpdatableViewModelBase
  {

    public ChannelAdvisorSalesDataViewModel()
    {
      this.Title = "Sales Data - ChannelAdvisor";
      this.Description = "Retrieves sales data from ChannelAdvisor.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "SalesDataChannelAdvisor";

      this.GetLastUpdated();

      this.UpdateFrequency = new UpdateFrequency(new TimeSpan(0, 0, 1, 0, 0), false);
    }

    protected override async Task UpdateAsync()
    {
      IsUpdatable = false;
      await Task.Run(() =>
      {
        try {
          this.Status = UpdatableStatus.Running;
          this.ProgressBarVisibility = System.Windows.Visibility.Visible;

          //Download sales data
          DownloadSalesData();

          //here, it should trigger anything that should happen if database fields are changed (recalculating)

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "Local sales data updated from ChannelAdvisor successfully.";
          this.ProgressBarVisibility = System.Windows.Visibility.Collapsed;
          this.StatusPercentage = 0;
          this.LastUpdated = DateTime.Now;

          //Update DB Statuses
          this.CommitServiceStatus();
        }
        catch (Exception e)
        {
          this.Status = UpdatableStatus.Error;
          this.StatusDescription = string.Format("{0}", e.Message);
          this.ProgressBarVisibility = System.Windows.Visibility.Collapsed;
        }
      });
      IsUpdatable = true;
    }

    private void DownloadSalesData()
    {
      try {
        this.StatusDescription = string.Format("Creating ChannelAdvisor Client");

        //Create OrderService client and ready it for request
        string devKey = System.Configuration.ConfigurationManager.AppSettings["CaDevK"];
        string devPW = System.Configuration.ConfigurationManager.AppSettings["CaDevPw"];
        //int profileID = 32001327;
        string accountID = System.Configuration.ConfigurationManager.AppSettings["CaAcct"]; ;
        APICredentials cred = new APICredentials();
        cred.DeveloperKey = devKey;
        cred.Password = devPW;
        OrderServiceSoapClient ordClient = new OrderServiceSoapClient();

        //Get latest order date in DB, and pull all orders since then
        DateTime beginTime;
        using (var db = new CheapeesEntities())
        {
          beginTime = (DateTime)db.MerchantFulfilledSales.OrderByDescending(o => o.OrderTime).FirstOrDefault().OrderTime;
        }

        OrderCriteria criteria = new OrderCriteria();
        criteria.OrderCreationFilterBeginTimeGMT = beginTime;
        criteria.OrderCreationFilterEndTimeGMT = DateTime.Now;
        criteria.DetailLevel = "High";
        List<ChannelAdvisorSale> listOfSales = new List<ChannelAdvisorSale>();

        int page = 1;
        criteria.PageNumberFilter = page;

        //Issue requests
        this.StatusDescription = string.Format("Requesting sales data since latest sale ({0})", beginTime);
        APIResultOfArrayOfOrderResponseItem response = ordClient.GetOrderList(cred, accountID, criteria);
        int numberOfOrders = 0;

        //When everything has been retrieved, pages will be returned empty and should exit loop
        while (response.ResultData.Length != 0)
        {
          numberOfOrders += response.ResultData.Length;
          this.StatusDescription = string.Format("Downloading sales data: {0} orders retrieved", numberOfOrders);
          foreach (OrderResponseDetailHigh order in response.ResultData)
          {
            foreach (OrderLineItemItem item in order.ShoppingCart.LineItemSKUList)
            {
              ChannelAdvisorSale sale = new ChannelAdvisorSale();
              sale.SKU = item.SKU;
              sale.Quantity = item.Quantity;
              sale.Marketplace = item.ItemSaleSource;
              sale.UnitPrice = item.UnitPrice;
              sale.OrderTime = ((DateTime)order.OrderTimeGMT).ToLocalTime();
              sale.Invoice = order.OrderID.ToString();

              listOfSales.Add(sale);
            }
          }

          //Get next page of results
          page++;
          criteria.PageNumberFilter = page;
          response = ordClient.GetOrderList(cred, accountID, criteria);
        }

        CommitToDatabase(listOfSales);
      }
      catch (Exception e)
      {
        throw new Exception(string.Format("ChannelAdvisorOrderService - {0}", e.Message));
      }
    }

    private void CommitToDatabase(List<ChannelAdvisorSale> sales)
    {
      using (var db = new CheapeesEntities())
      {
        foreach (var sale in sales)
        {
          if (db.MerchantFulfilledSales.Local.Count(o => (o.Invoice.Equals(sale.Invoice) && o.Sku.Equals(sale.SKU))) == 0 && db.MerchantFulfilledSales.Count(o => (o.Invoice.Equals(sale.Invoice) && o.Sku.Equals(sale.SKU))) == 0)
          {
            MerchantFulfilledSale dbSale = new MerchantFulfilledSale();
            dbSale.Invoice = sale.Invoice;
            dbSale.Marketplace = sale.Marketplace;
            dbSale.OrderTime = sale.OrderTime;
            dbSale.Quantity = sale.Quantity;
            dbSale.Sku = sale.SKU;
            dbSale.UnitPrice = sale.UnitPrice;

            db.MerchantFulfilledSales.Add(dbSale);
          }

        }
        db.SaveChanges();
      }
    }
  }

  class ChannelAdvisorSale
  {
    //All details are on an order level. The price reflects the actual price paid by the customer.
    public string SKU { get; set; }
    public int Quantity { get; set; }
    public string Marketplace { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime OrderTime { get; set; }
    public string Invoice { get; set; }

    public ChannelAdvisorSale() { }
  }
}
