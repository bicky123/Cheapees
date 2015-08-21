using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cheapees
{
  /// <summary>
  /// The StatusControl is a user control designed to represent an service which runs updates.
  /// </summary>
  public partial class StatusControl : UserControl
  {

    /// <summary>
    /// Constructor for the StatusControl.
    /// </summary>
    /// <param name="context">The UpdatableViewModel that will bind with this view.</param>
    public StatusControl(UpdatableViewModelBase context)
    {
      InitializeComponent();
      this.DataContext = context;
    }
  }
}
