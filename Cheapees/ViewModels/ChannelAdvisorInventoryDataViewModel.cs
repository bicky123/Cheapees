using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
      throw new NotImplementedException();
    }
  }
}
