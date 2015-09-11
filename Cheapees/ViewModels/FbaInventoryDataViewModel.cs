using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MarketplaceWebService;
using System.IO;

namespace Cheapees
{
  public class FbaInventoryDataViewModel : UpdatableViewModelBase
  {

    public int SecondsTilRetry = 60;

    public FbaInventoryDataViewModel()
    {
      this.Title = "Inventory Data - FBA";
      this.Description = "Retrieves current inventory being Fulfilled-By-Amazon.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "InventoryDataFba";
      this.GetLastUpdated();

      this.UpdateFrequency = new UpdateFrequency(new TimeSpan(0, 4, 0, 0, 0), false);
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
          this.StatusDescription = "All FBA inventory data downloaded successfully.";
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

      this.StatusDescription = string.Format("Creating MWS client");
      // Set up MWS client
      string accessKey = System.Configuration.ConfigurationManager.AppSettings["MwsAccK"];
      string secretKey = System.Configuration.ConfigurationManager.AppSettings["MwsSecK"];
      string sellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
      string marketplaceId = System.Configuration.ConfigurationManager.AppSettings["MwsMktpl"];
      MarketplaceWebServiceConfig config = new MarketplaceWebServiceConfig();
      config.ServiceURL = "https://mws.amazonservices.com ";
      config.SetUserAgentHeader("CMA", "1.0", "C#", new string[] { });
      MarketplaceWebServiceClient client = new MarketplaceWebServiceClient(accessKey, secretKey, config);


      // Submit the report request
      MarketplaceWebService.Model.RequestReportRequest request = new MarketplaceWebService.Model.RequestReportRequest();
      request.ReportType = "_GET_FBA_MYI_ALL_INVENTORY_DATA_";
      request.Merchant = sellerId;
      request.StartDate = DateTime.Now.AddDays(-30);
      request.EndDate = DateTime.Now.AddDays(-1);

      this.StatusDescription = string.Format("Requesting Report Type {0} for {1} through {2}", request.ReportType, request.StartDate.ToShortDateString(), request.EndDate.ToShortDateString());
      var responseToRequestReport = client.RequestReport(request);

      Thread.Sleep(1000);

      string reportRequestId = "";

      if (responseToRequestReport.IsSetRequestReportResult())
      {
        if (responseToRequestReport.RequestReportResult.IsSetReportRequestInfo())
        {
          if (responseToRequestReport.RequestReportResult.ReportRequestInfo.IsSetReportRequestId())
          {
            reportRequestId = responseToRequestReport.RequestReportResult.ReportRequestInfo.ReportRequestId;
          }
          else
          {
            // Would be good to implement wait-and-retry here
            throw new Exception("ReportRequestId was not returned from RequestReport()");
          }
        }
      }

      this.StatusDescription = string.Format("Report Request ID: {0}", reportRequestId);
      Thread.Sleep(1000);

      // Check for the report to have a _DONE_ status
      string reportId = "";

      bool reportDone = false;

      do
      {
        this.StatusDescription = string.Format("Checking report status");
        MarketplaceWebService.Model.GetReportRequestListRequest requestGetReportRequestList = new MarketplaceWebService.Model.GetReportRequestListRequest();
        requestGetReportRequestList.ReportRequestIdList = new MarketplaceWebService.Model.IdList();
        requestGetReportRequestList.ReportRequestIdList.Id = new List<string>();
        requestGetReportRequestList.ReportRequestIdList.Id.Add(reportRequestId);
        requestGetReportRequestList.Merchant = sellerId;

        var responseToGetReportRequestList = client.GetReportRequestList(requestGetReportRequestList);

        if (responseToGetReportRequestList.IsSetGetReportRequestListResult() && responseToGetReportRequestList.GetReportRequestListResult.IsSetReportRequestInfo() && responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo.Count != 0)
        {
          this.StatusDescription = string.Format("Report status: {0}", responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].ReportProcessingStatus);
          Thread.Sleep(500);
          if (responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].ReportProcessingStatus.Equals("_DONE_"))
          {
            reportDone = true;
            if (responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].IsSetGeneratedReportId())
            {
              reportId = responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].GeneratedReportId;
              this.StatusDescription = string.Format("Report ID: {0}", reportId);
            }
            else
            {
              // ReportId was not returned, call GetReportList()
              this.StatusDescription = string.Format("Report ID was not returned. Trying GetReportList()");
              var requestGetReportList = new MarketplaceWebService.Model.GetReportListRequest();
              requestGetReportList.ReportRequestIdList = new MarketplaceWebService.Model.IdList();
              requestGetReportList.ReportRequestIdList.Id = new List<string>();
              requestGetReportList.ReportRequestIdList.Id.Add(reportRequestId);
              requestGetReportList.Merchant = sellerId;

              var responseToGetReportList = client.GetReportList(requestGetReportList);

              if (responseToGetReportList.IsSetGetReportListResult() && responseToGetReportList.GetReportListResult.IsSetReportInfo() && responseToGetReportList.GetReportListResult.ReportInfo.Count != 0)
              {
                reportId = responseToGetReportList.GetReportListResult.ReportInfo[0].ReportId;
                this.StatusDescription = string.Format("Report ID: {0}", reportId);
              }
              else
              {
                throw new Exception("ReportId could not be retrieved.");
              }
            }
          }
          else if (responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].ReportProcessingStatus.Equals("_DONE_NO_DATA_"))
          {
            throw new Exception("Report returned with status _DONE_NO_DATA_");
          }
          else if (responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].ReportProcessingStatus.Equals("_CANCELLED_"))
          {
            throw new Exception("Report returned with status _CANCELLED_");
          }
          else
          {
            for (int secondsTilRetry = SecondsTilRetry; secondsTilRetry > 0; secondsTilRetry--)
            {
              this.StatusDescription = string.Format("Report status: {0} (Will check again in {1}s)", responseToGetReportRequestList.GetReportRequestListResult.ReportRequestInfo[0].ReportProcessingStatus, secondsTilRetry);
              Thread.Sleep(1000);
            }
          }
        }

      } while (!reportDone);

      Thread.Sleep(1000);

      // Retrieve the report
      string saveFileName = reportId + ".txt";
      this.StatusDescription = string.Format("Downloading report to {0}", saveFileName);
      MarketplaceWebService.Model.GetReportRequest requestGetReport = new MarketplaceWebService.Model.GetReportRequest();
      requestGetReport.ReportId = reportId;
      requestGetReport.Merchant = sellerId;

      using (Stream file = File.Open(saveFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
      {
        requestGetReport.Report = file;
        var responseToGetReport = client.GetReport(requestGetReport);
      }

      // Update DB with content of report
      this.StatusDescription = string.Format("Loading inventory data from report file");
      var fbaQuantities = LoadInventoryFromFile(saveFileName);

      this.StatusDescription = string.Format("Saving data to database");
      CommitToDatabase(fbaQuantities);
    }

    public List<FbaInventoryQuantity> LoadInventoryFromFile(string fileName)
    {
      List<FbaInventoryQuantity> fbaQuantities = new List<FbaInventoryQuantity>();

      using (StreamReader file = new StreamReader(fileName))
      {
        string[] headers = file.ReadLine().Split('\t');

        int colSku = -1;
        int colAsin = -1;
        int colFnsku = -1;
        int colFulfillable = -1;
        int colTotal = -1;
        int colInboundShipped = -1;
        int colInboundWorking = -1;
        int colInboundReceiving = -1;

        // Find headers' column indexes
        for (int i = 0; i < headers.Length; i++)
        {
          if (headers[i].Equals("sku"))
            colSku = i;
          else if (headers[i].Equals("asin"))
            colAsin = i;
          else if (headers[i].Equals("fnsku"))
            colFnsku = i;
          else if (headers[i].Equals("afn-fulfillable-quantity"))
            colFulfillable = i;
          else if (headers[i].Equals("afn-total-quantity"))
            colTotal = i;
          else if (headers[i].Equals("afn-inbound-working-quantity"))
            colInboundWorking = i;
          else if (headers[i].Equals("afn-inbound-shipped-quantity"))
            colInboundShipped = i;
          else if (headers[i].Equals("afn-inbound-receiving-quantity"))
            colInboundReceiving = i;
        }
        
        string line;

        while ((line = file.ReadLine()) != null)
        {
          FbaInventoryQuantity fbaQuantity = new FbaInventoryQuantity();
          string[] delimitedLine = line.Split('\t');
          fbaQuantity.Sku = delimitedLine[colSku];
          fbaQuantity.Asin = delimitedLine[colAsin];
          fbaQuantity.Fnsku = delimitedLine[colFnsku];
          fbaQuantity.FulfillableQuantity = int.Parse(delimitedLine[colFulfillable]);
          fbaQuantity.TotalQuantity = int.Parse(delimitedLine[colTotal]);
          fbaQuantity.InboundWorkingQuantity = int.Parse(delimitedLine[colInboundWorking]);
          fbaQuantity.InboundShippedQuantity = int.Parse(delimitedLine[colInboundShipped]);
          fbaQuantity.InboundReceivingQuantity = int.Parse(delimitedLine[colInboundReceiving]);

          fbaQuantities.Add(fbaQuantity);
        }
      }
      return fbaQuantities;
    }

    public void CommitToDatabase(List<FbaInventoryQuantity> fbaQuantities)
    {
      using (var db = new CheapeesEntities())
      {
        db.Configuration.AutoDetectChangesEnabled = false;
        db.Configuration.ValidateOnSaveEnabled = false;

        // Clear table first
        db.AmazonFulfilledInventories.RemoveRange(db.AmazonFulfilledInventories);
        db.SaveChanges();

        for (int i = 0; i < fbaQuantities.Count; i++)
        {
          var qty = fbaQuantities[i];
          AmazonFulfilledInventory dbInv = new AmazonFulfilledInventory();
          dbInv.Asin = qty.Asin;
          dbInv.FbaSku = qty.Sku;
          dbInv.Fnsku = qty.Fnsku;
          dbInv.FulfillableQuantity = qty.FulfillableQuantity;
          dbInv.InboundReceivingQuantity = qty.InboundReceivingQuantity;
          dbInv.InboundShippedQuantity = qty.InboundShippedQuantity;
          dbInv.InboundWorkingQuantity = qty.InboundWorkingQuantity;
          dbInv.TotalQuantity = qty.TotalQuantity;

          db.AmazonFulfilledInventories.Add(dbInv);

          if (i % 1000 == 0)
          {
            this.StatusDescription = string.Format("Committing to database - {0:0.00}% ({1}/{2})", i * 100.0 / fbaQuantities.Count, i, fbaQuantities.Count);
            this.StatusPercentage = i * 100 / fbaQuantities.Count;
            db.SaveChanges();
          }

        }

        db.SaveChanges();
      }
    }
  }

  public class FbaInventoryQuantity
  {
    public string Sku;
    public string Asin;
    public string Fnsku;

    public int FulfillableQuantity;
    public int TotalQuantity;
    public int InboundShippedQuantity;
    public int InboundWorkingQuantity;
    public int InboundReceivingQuantity;
  }
}
