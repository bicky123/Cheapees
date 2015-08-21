using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cheapees
{
  public class UpdateFrequency
  {

    private TimeSpan _updateThreshold;
    public TimeSpan UpdateThreshold
    {
      get { return _updateThreshold; }
      set
      {
        _updateThreshold = value;
      }
    }

    private bool _shouldUpdateDaily;
    public bool ShouldUpdateDaily
    {
      get { return _shouldUpdateDaily; }
      set
      {
        _shouldUpdateDaily = value;
      }
    }

    public UpdateFrequency(TimeSpan updateThreshold, bool updateDaily)
    {
      UpdateThreshold = updateThreshold;
      ShouldUpdateDaily = updateDaily;
    }

    /// <summary>
    /// Determines whether an update is needed based on the last update time.
    /// </summary>
    /// <param name="lastUpdate">A DateTime representing the last update time.</param>
    /// <returns></returns>
    public bool UpdateNeeded(DateTime lastUpdate)
    {
      if (ShouldUpdateDaily)
      {
        if (!lastUpdate.Date.Equals(DateTime.Now.Date))
          return true;
        else
          return false;
      }
      else if (DateTime.Now - lastUpdate > UpdateThreshold)
        return true;
      
      return false;
    }
  }
}
