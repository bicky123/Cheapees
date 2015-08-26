using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cheapees.ChannelAdvisorInventoryService;

namespace Cheapees
{
  public class ChannelAdvisorInventoryDataViewModel : UpdatableViewModelBase
  {

    public ChannelAdvisorInventoryDataViewModel()
    {
      this.Title = "Inventory Data - ChannelAdvisor";
      this.Description = "Retrieves all inventory details from ChannelAdvisor.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "InventoryDataChannelAdvisor";
      this.GetLastUpdated();

      this.UpdateFrequency = new UpdateFrequency(new TimeSpan(0, 0, 0, 0, 0), true);
    }

    protected override async Task UpdateAsync()
    {
      await Task.Run(() =>
      {
        IsUpdatable = false;

        try
        {
          this.Status = UpdatableStatus.Running;
          this.ProgressBarVisibility = System.Windows.Visibility.Visible;

          //Download data
          DownloadInventoryData();

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "All ChannelAdvisor inventory data downloaded successfully.";
          this.ProgressBarVisibility = System.Windows.Visibility.Collapsed;
          this.StatusPercentage = 0;
          this.LastUpdated = DateTime.Now;

          this.CommitServiceStatus();
        }
        catch (Exception e)
        {
          this.Status = UpdatableStatus.Error;
          this.StatusDescription = string.Format("{0}", e.Message);
          this.ProgressBarVisibility = System.Windows.Visibility.Collapsed;
        }

        IsUpdatable = true;
      });
    }

    private void DownloadInventoryData()
    {

      try
      {
        this.StatusDescription = string.Format("Creating ChannelAdvisor Client");

        //Create OrderService client and ready it for request
        string devKey = System.Configuration.ConfigurationManager.AppSettings["CaDevK"];
        string devPW = System.Configuration.ConfigurationManager.AppSettings["CaDevPw"];
        //int profileID = 32001327;
        string accountID = System.Configuration.ConfigurationManager.AppSettings["CaAcct"]; ;
        APICredentials cred = new APICredentials();
        cred.DeveloperKey = devKey;
        cred.Password = devPW;
        InventoryServiceSoapClient invClient = new InventoryServiceSoapClient();

        this.StatusDescription = string.Format("Downloading Inventory Item List");

        int invItemDownloadedCount = 0;
        List<InventoryItem> inventory = new List<InventoryItem>();

        InventoryItemCriteria itemCriteria = new InventoryItemCriteria();
        itemCriteria.PageNumber = 1;
        itemCriteria.PageSize = 100;

        InventoryItemDetailLevel detailLevel = new InventoryItemDetailLevel();
        detailLevel.IncludePriceInfo = true;
        detailLevel.IncludeQuantityInfo = true;
        detailLevel.IncludeClassificationInfo = false;

        APIResultOfArrayOfInventoryItemResponse response;

        while ((response = invClient.GetFilteredInventoryItemList(cred, accountID, itemCriteria, detailLevel, "", "")).ResultData.Length != 0)
        {
          //invItemDownloadedCount += response.ResultData.Length;
          //this.StatusDescription = string.Format("Downloading Inventory Item List ({0} completed)", invItemDownloadedCount);

          foreach (var responseItem in response.ResultData)
          {
            invItemDownloadedCount++;
            this.StatusDescription = string.Format("Downloading Inventory Item List ({0} completed)", invItemDownloadedCount);
            InventoryItem item = new InventoryItem();
            item.Asin = responseItem.ASIN;
            item.Brand = responseItem.Brand;
            item.Description = responseItem.Description;
            item.Ean = responseItem.EAN;
            item.HarmonizedCode = responseItem.HarmonizedCode;
            item.Height = (decimal)responseItem.Height;
            item.Isbn = responseItem.ISBN;
            item.Length = (decimal)responseItem.Length;
            item.Manufacturer = responseItem.Manufacturer;
            item.Mpn = responseItem.MPN;
            item.Quantity = responseItem.Quantity.Total;
            item.Sku = responseItem.Sku;
            item.SupplierCode = responseItem.SupplierCode;
            item.Title = responseItem.Title;
            item.Upc = responseItem.UPC;
            item.WarehouseLocation = responseItem.WarehouseLocation;
            item.Weight = (decimal)responseItem.Weight;
            item.Width = (decimal)responseItem.Width;

            var attrResponse = invClient.GetInventoryItemAttributeList(cred, accountID, item.Sku);
            foreach (var attrPair in attrResponse.ResultData)
            {
              item.AttributeList.Add(attrPair.Name, attrPair.Value);
            }

            inventory.Add(item);
          }

          itemCriteria.PageNumber += 1;
        }
      }
      catch (Exception e)
      {
        throw new Exception(string.Format("ChannelAdvisorInventoryService - {0}", e.Message));
      }

    }
  }

  public class InventoryItem
  {
    public string Sku, Title, Description, SupplierCode, WarehouseLocation, Asin, Isbn, Upc, Ean, Mpn, Manufacturer, Brand, HarmonizedCode;
    
    public decimal Weight, Height, Length, Width;

    public int Quantity;

    public Dictionary<String, String> AttributeList;

    public InventoryItem()
    {
      AttributeList = new Dictionary<string, string>();
    }
  }
}
