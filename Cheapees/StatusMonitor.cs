using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cheapees
{
  /// <summary>
  /// The StatusMonitor class accepts a list of Updatable View Models and continuously checks if an update is required.
  /// </summary>
  public class StatusMonitor : BindableBase
  {
    //Properties
    private TimeSpan _checkFrequency;
    public TimeSpan CheckFrequency
    {
      get { return _checkFrequency; }
      set
      {
        _checkFrequency = value;
        RaisePropertyChanged();
      }
    }

    public List<UpdatableViewModelBase> Services;

    //Constructors
    public StatusMonitor(List<UpdatableViewModelBase> services)
    {
      Services = services;
      CheckFrequency = new TimeSpan(0, 5, 0);
    }

    public async void BeginChecking()
    {
      await BeginCheckingAsync();
    }

    public async Task BeginCheckingAsync()
    {
      await Task.Run(() =>
      {
        while (true)
        {
          foreach (var s in Services)
          {
            if (s.IsUpdatable)
            {
              if (s.UpdateFrequency.UpdateNeeded(s.LastUpdated))
              {
                s.Status = UpdatableStatus.UpdateNeeded;
                TimeSpan ts = s.UpdateFrequency.UpdateThreshold;
                s.StatusDescription = string.Format("This service hasn't been updated since {0}, which exceeds the required update frequency ({1})", s.LastUpdated, (s.UpdateFrequency.ShouldUpdateDaily ? "daily" : (string.Format("{0}d {1}h {2}m {3}s",ts.Days, ts.Hours, ts.Minutes, ts.Seconds))));
              }
            }
          }

          Thread.Sleep(CheckFrequency);
        }

      });
    }

  }
}
