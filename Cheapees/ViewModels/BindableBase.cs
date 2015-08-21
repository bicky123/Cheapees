using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cheapees
{
  public abstract class BindableBase : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected void RaisePropertyChanged([CallerMemberName] string caller = "")
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(caller));
      }
    }
  }
}
