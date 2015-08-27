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
using System.Threading;

namespace Cheapees
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      ChannelAdvisorInventoryDataViewModel caInventoryVm = new ChannelAdvisorInventoryDataViewModel();
      StatusControl caInventoryVmStatus = new StatusControl(caInventoryVm);
      caInventoryVmStatus.exp.IsExpanded = true;
      stkStatuses.Children.Add(caInventoryVmStatus);

      FbaInventoryDataViewModel fbaInventoryVm = new FbaInventoryDataViewModel();
      StatusControl fbaInventoryVmStatus = new StatusControl(fbaInventoryVm);
      fbaInventoryVmStatus.exp.IsExpanded = true;
      stkStatuses.Children.Add(fbaInventoryVmStatus);

      VendorDataFtpViewModel ftpVm = new VendorDataFtpViewModel();
      StatusControl ftpVmStatus = new StatusControl(ftpVm);
      ftpVmStatus.exp.IsExpanded = true;
      stkStatuses.Children.Add(ftpVmStatus);

      AmazonListingDataViewModel amzListingVm = new AmazonListingDataViewModel();
      StatusControl amzListingVmStatus = new StatusControl(amzListingVm);
      amzListingVmStatus.exp.IsExpanded = true;
      stkStatuses.Children.Add(amzListingVmStatus);

      ChannelAdvisorSalesDataViewModel caSalesVm = new ChannelAdvisorSalesDataViewModel();
      StatusControl caSalesVmStatus = new StatusControl(caSalesVm);
      caSalesVmStatus.exp.IsExpanded = true;
      stkStatuses.Children.Add(caSalesVmStatus);

      FbaSalesDataViewModel fbaSalesVm = new FbaSalesDataViewModel();
      StatusControl fbaSalesVmStatus = new StatusControl(fbaSalesVm);
      fbaSalesVmStatus.exp.IsExpanded = true;
      stkStatuses.Children.Add(fbaSalesVmStatus);

      //Initial Updates
      //ftpVm.Update();
      //caSalesVm.Update();
      //amzListingVm.Update();
      //fbaSalesVm.Update();
      //caInventoryVm.Update();
      //fbaInventoryVm.Update();

      List<UpdatableViewModelBase> viewModels = new List<UpdatableViewModelBase>();
      //viewModels.Add(ftpVm);
      //viewModels.Add(caSalesVm);
      //viewModels.Add(amzListingVm);
      //viewModels.Add(fbaSalesVm);
      //viewModels.Add(caInventoryVm);
      viewModels.Add(fbaInventoryVm);

      StatusMonitor monitor = new StatusMonitor(viewModels);
      monitor.BeginChecking();
    }
  }
}
