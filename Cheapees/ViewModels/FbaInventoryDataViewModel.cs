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
          DownloadSalesData();

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

    private void DownloadSalesData()
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
    }
  }
}
