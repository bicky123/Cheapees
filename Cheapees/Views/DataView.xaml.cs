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
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Cheapees
{
  /// <summary>
  /// Interaction logic for DataView.xaml
  /// </summary>
  public partial class DataView : UserControl
  {
    public DataView()
    {
      GraphSalesRank graphSr = new GraphSalesRank();

      InitializeComponent();

      stkGraphs.Children.Add(graphSr);
    }
  }
}
