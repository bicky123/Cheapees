using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cheapees
{
  public class AmazonListingDataViewModel : UpdatableViewModelBase
  {

    public AmazonListingDataViewModel()
    {
      this.Title = "Listing Data - Amazon (SalesRank, BuyBox)";
      this.Description = "Updates SalesRank and BuyBox data for all ASINs in database.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "AmazonListingData";

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

          //Start Updating
          DownloadListingData();

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "All Amazon listings retrieved successfully.";
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

    private void DownloadListingData()
    {
      throw new NotImplementedException();
    }
  }
}
