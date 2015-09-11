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
  /// Interaction logic for GraphSalesRank.xaml
  /// </summary>
  public partial class GraphSalesRank : UserControl
  {
    public PlotModel model { get; set; }

    private LineSeries series { get; set; }

    private string _asin;
    public string Asin
    {
      get
      {
        return _asin;
      }
      set
      {
        _asin = value;
        this.UpdateData();
      }
    }

    public void UpdateData()
    {
      if (string.IsNullOrEmpty(this.Asin))
        return;
      List<AmazonListing> srList;
      using (var db = new CheapeesEntities())
      {
        srList = db.AmazonListings.Where(o => o.Asin.Equals(this.Asin)).ToList();
      }

      series.Points.Clear();

      foreach (var sr in srList)
      {
        series.Points.Add(new DataPoint(sr.Date.ToOADate(), (double)sr.SalesRank));
      }

    }

    public GraphSalesRank()
    {
      model = new PlotModel();
      model.Title = "SalesRank";
      
      DateTimeAxis axisX = new DateTimeAxis();
      axisX.Position = AxisPosition.Bottom;
      axisX.Title = "Date";
      model.Axes.Add(axisX);

      LinearAxis axisY = new LinearAxis();
      axisY.Position = AxisPosition.Left;
      axisY.Minimum = 0;
      axisY.Title = "SalesRank";
      model.Axes.Add(axisY);


      series = new LineSeries();
      series.Title = string.Format("ASIN: {0}", this.Asin);

      this.Asin = "0061670898";

      model.Series.Add(series);

      InitializeComponent();

      oxyPlotView.Model = model;
    }
  }
}
