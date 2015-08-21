using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cheapees
{
  public class FbaSalesDataViewModel : UpdatableViewModelBase
  {

    public FbaSalesDataViewModel()
    {
      this.Title = "Sales Data - FBA";
      this.Description = "Retrieves sales data for orders Fulfilled-By-Amazon.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "SalesDataFba";

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

          //Download sales data
          DownloadSalesData();

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "All FBA sales data downloaded successfully.";
          this.ProgressBarVisibility = System.Windows.Visibility.Collapsed;
          this.StatusPercentage = 0;
          this.LastUpdated = DateTime.Now;
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
      //use MWS Reports

      //reportRequestId = RequestReport()

      //while (GetReportRequestList(reportRequestId) != _DONE_)

      //if (GetReportRequestList did not return generated ReportId)
      //reportId = GetReportList(ReportRequestId)

      //report = GetReport(reportId)

      throw new NotImplementedException();
    }
  }
}
