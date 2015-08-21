using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cheapees
{
  /// <summary>
  /// UpdatableViewModelBase forms a a base class for all Updatable ViewModels.
  /// These ViewModels are expected to have bindable properties (such as Title, Status, and StatusDescription)
  /// that represent the status of the updatable service which implements this.
  /// </summary>
  public abstract class UpdatableViewModelBase : BindableBase
  {

    //Bindable Properties
    private string _title;
    /// <summary>
    /// Displayable title of this service.
    /// </summary>
    public string Title
    {
      get { return _title; }
      set
      {
        _title = value;
        RaisePropertyChanged();
      }
    }

    private string _description;
    /// <summary>
    /// Displayable description of this service.
    /// </summary>
    public string Description
    {
      get { return _description; }
      set
      {
        _description = value;
        RaisePropertyChanged();
      }
    }

    private UpdatableStatus _status;
    /// <summary>
    /// Current status of this service (in regards to updates).
    /// </summary>
    public UpdatableStatus Status
    {
      get { return _status; }
      set
      {
        _status = value;

        //not working?
        if (value == UpdatableStatus.Ok)
        {
          StatusImage = "/Cheapees;component/Resources/Images/led_green.png";
          ProgressBarVisibility = System.Windows.Visibility.Collapsed;
        }
        else if (value == UpdatableStatus.Error || value == UpdatableStatus.UpdateNeeded)
        {
          StatusImage = "/Cheapees;component/Resources/Images/led_red.png";
          ProgressBarVisibility = System.Windows.Visibility.Collapsed;
        }
        else if (value == UpdatableStatus.Running)
        {
          StatusImage = "/Cheapees;component/Resources/Images/led_yellow.png";
          ProgressBarVisibility = System.Windows.Visibility.Visible;
        }
        else
        {
          StatusImage = "/Cheapees;component/Resources/Images/led_grey.png";
          ProgressBarVisibility = System.Windows.Visibility.Collapsed;
        }

        RaisePropertyChanged();
      }
    }

    private string _statusImage;
    /// <summary>
    /// A visual representation of this service's current Status. This property is set by the Status property's setter.
    /// </summary>
    public string StatusImage
    {
      get
      {
        return _statusImage;
      }
      set
      {
        _statusImage = value;
        RaisePropertyChanged();
      }
    }

    private string _statusDescription;
    /// <summary>
    /// Details regarding the current Status of this service.
    /// </summary>
    public string StatusDescription
    {
      get { return _statusDescription; }
      set
      {
        _statusDescription = string.Format("{0} - {1}", Status.ToString(), value);
        RaisePropertyChanged();
      }
    }

    private int _statusPercentage;
    /// <summary>
    /// Current progress of this service's update, bindable to a progress bar.
    /// </summary>
    public int StatusPercentage
    {
      get { return _statusPercentage; }
      set
      { if (value >= 0 && value <= 100)
          _statusPercentage = value;
        else
          _statusPercentage = 0;

        RaisePropertyChanged();
      }
    }

    private System.Windows.Visibility _progressBarVisibility;
    /// <summary>
    /// Visibility of a progress bar bound to this service. This property is set by the Status property's setter.
    /// </summary>
    public System.Windows.Visibility ProgressBarVisibility
    {
      get { return _progressBarVisibility; }
      set
      {
        _progressBarVisibility = value;
        RaisePropertyChanged();
      }
    }

    private DateTime _lastUpdated;
    /// <summary>
    /// DateTime representing when this service was last successfully updated.
    /// </summary>
    public DateTime LastUpdated
    {
      get
      {
        return _lastUpdated;
      }
      set
      {
        _lastUpdated = value;
        RaisePropertyChanged();
      }
    }

    private UpdateFrequency _updateFrequency; //should be a setting
    /// <summary>
    /// UpdateFrequency describing how often this service should be updated.
    /// </summary>
    public UpdateFrequency UpdateFrequency
    {
      get { return _updateFrequency; }
      set
      {
        _updateFrequency = value;
      }
    }

    private bool _isUpdatable; //whether any setting here can be changed should depend upon this
    /// <summary>
    /// Describes when the service is available to begin Updating. This should return true when the service update is not running, but runnable.
    /// </summary>
    public bool IsUpdatable
    {
      get { return _isUpdatable; }
      set
      {
        _isUpdatable = value;
        RaisePropertyChanged();
      }
    }

    //Private Property
    /// <summary>
    /// A magic string representing this service, to be used in storing meta-data in a database.
    /// </summary>
    protected string ServiceId;

    /// <summary>
    /// How often the UI should be updated. This property should be checked before any StatusDescription or StatusPercentage changes that should be throttled.
    /// (Useful for example in cases where the status changes rapidly enough as to slow down the UI thread with RaisePropertyChanged() calls.)
    /// </summary>
    protected TimeSpan _uiUpdateThreshold; //should be a setting

    //Constructor
    protected UpdatableViewModelBase()
    {
      Status = UpdatableStatus.Unknown;
      StatusDescription = "This module has not attempted to run yet.";
      StatusPercentage = 0;
      ProgressBarVisibility = System.Windows.Visibility.Collapsed;
      _isUpdatable = true;
      //GetLastUpdated();
    }

    /// <summary>
    /// Retrieves the last updated time from the database.
    /// </summary>
    public void GetLastUpdated()
    {
      //use ServiceId to look up the last update DateTime
      throw new NotImplementedException();
    }

    /// <summary>
    /// The base class function that can be called to launch the (presumably) implementing-class-specific code in UpdateAsync().
    /// </summary>
    public async void Update()
    {
      await UpdateAsync();
    }

    /// <summary>
    /// This virtual function should be overridden by any implementing class to perform it's specific functionality asynchronously.
    /// </summary>
    /// <returns>An awaitable Task.</returns>
    protected virtual async Task UpdateAsync()
    {
      //General way of handling the Async Update
      await Task.Run(() =>
      {
        this.Status = UpdatableStatus.Running;
        try {
          //Fill in service code here
          //-------------------------
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = string.Format("Finished running successfully at {0}.",DateTime.Now);
          this.StatusPercentage = 100;
          this.LastUpdated = DateTime.Now;
          throw new Exception("Asynchronous update method has not been overridden in subclass.");
        }
        catch (Exception e)
        {
          //Fill in error handling code here
          //--------------------------------
          this.Status = UpdatableStatus.Error;
          this.StatusDescription = string.Format("{0}",e.Message);
        }
      });
    }
    
  }

  public enum UpdatableStatus { Ok, Running, Error, UpdateNeeded, Unknown }

}