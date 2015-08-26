using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;

namespace Cheapees
{
  public class VendorDataFtpViewModel : UpdatableViewModelBase
  {
    public VendorDataFtpViewModel()
    {
      this.Title = "Inventory Data - Vendor FTP";
      this.Description = "Retrieves vendor data through FTP downloads and updates the database.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "VendorDataFtp";
      this.GetLastUpdated();

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
          
          //Europa
          DownloadEuropaFtp();

          //Lonestar
          DownloadLonestarFtp();

          //Database
          //CommitToDatabase();

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "All FTP data downloaded successfully.";
          this.ProgressBarVisibility = System.Windows.Visibility.Collapsed;
          this.StatusPercentage = 0;
          this.LastUpdated = DateTime.Now;

          this.CommitServiceStatus();
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

    public void DownloadEuropaFtp()
    {
      try {
        string remoteFtpPath = System.Configuration.ConfigurationManager.AppSettings["FtpEsAddr"];
        string username = System.Configuration.ConfigurationManager.AppSettings["FtpEsUn"];
        string password = System.Configuration.ConfigurationManager.AppSettings["FtpEsPw"];
        string localFilePath = "EuropaData.csv";

        long fileSize;

        //Get File Size
        this.StatusDescription = string.Format("(1/3) Europa - Getting File Size");
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteFtpPath);
        request.Method = WebRequestMethods.Ftp.GetFileSize;
        request.Credentials = new NetworkCredential(username, password);

        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        fileSize = response.ContentLength;

        //Download File
        this.StatusDescription = string.Format("(1/3) Europa - Downloading {0}", remoteFtpPath);
        request = (FtpWebRequest)WebRequest.Create(remoteFtpPath);
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = new NetworkCredential(username, password);

        response = (FtpWebResponse)request.GetResponse();

        Stream responseStream = response.GetResponseStream();
        StreamReader reader = new StreamReader(responseStream);

        using (FileStream writer = new FileStream(localFilePath, FileMode.Create))
        {
          long downloadedSize = 0;
          long length = response.ContentLength;
          int bufferSize = 2048;
          int readCount;
          byte[] buffer = new byte[bufferSize];

          DateTime lastStatusUpdate = DateTime.Now;

          readCount = responseStream.Read(buffer, 0, bufferSize);
          downloadedSize += readCount;
          while (readCount > 0)
          {
            writer.Write(buffer, 0, readCount);
            readCount = responseStream.Read(buffer, 0, bufferSize);
            downloadedSize += readCount;


            if (DateTime.Now - lastStatusUpdate > _uiUpdateThreshold)
            {
              this.StatusDescription = string.Format("(1/3) Europa - Downloading File - {0:n} KB of {1:n} KB", downloadedSize / 1000.0, fileSize / 1000.0);
              this.StatusPercentage = (int)(downloadedSize * 100.0 / fileSize);
              lastStatusUpdate = DateTime.Now;
            }

          }
        }
        reader.Close();
        response.Close();
      }
      catch (Exception e)
      {
        throw new Exception(string.Format("Europa - {0}", e.Message));
      }
    }

    public void DownloadLonestarFtp()
    {
      try {
        string remoteFtpPath = System.Configuration.ConfigurationManager.AppSettings["FtpLsAddr"];
        string username = System.Configuration.ConfigurationManager.AppSettings["FtpLsUn"];
        string password = System.Configuration.ConfigurationManager.AppSettings["FtpLsPw"]; ;
        string localFilePath = "LonestarData.csv";

        long fileSize;
        FtpWebRequest request;
        FtpWebResponse response;


        //Get File Size
        this.StatusDescription = string.Format("(2/3) Lonestar - Getting File Size");
        request = (FtpWebRequest)WebRequest.Create(remoteFtpPath);
        request.Method = WebRequestMethods.Ftp.GetFileSize;
        request.Credentials = new NetworkCredential(username, password);

        try {
          response = (FtpWebResponse)request.GetResponse();
          fileSize = response.ContentLength;
        }
        catch
        {
          fileSize = 22000000; //GetFileSize operation not permitted in Lonestar, but try anyway. This value is an average estimate.
        }


        //Download File
        this.StatusDescription = string.Format("(2/3) Lonestar - Downloading {0}", remoteFtpPath);
        request = (FtpWebRequest)WebRequest.Create(remoteFtpPath);
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = new NetworkCredential(username, password);

        response = (FtpWebResponse)request.GetResponse();

        Stream responseStream = response.GetResponseStream();
        StreamReader reader = new StreamReader(responseStream);

        using (FileStream writer = new FileStream(localFilePath, FileMode.Create))
        {
          long downloadedSize = 0;
          long length = response.ContentLength;
          int bufferSize = 2048;
          int readCount;
          byte[] buffer = new byte[bufferSize];

          DateTime lastStatusUpdate = DateTime.Now;

          readCount = responseStream.Read(buffer, 0, bufferSize);
          downloadedSize += readCount;
          while (readCount > 0)
          {
            writer.Write(buffer, 0, readCount);
            readCount = responseStream.Read(buffer, 0, bufferSize);
            downloadedSize += readCount;


            if (DateTime.Now - lastStatusUpdate > _uiUpdateThreshold)
            {
              this.StatusDescription = string.Format("(2/3) Lonestar - Downloading File - {0:n} KB of {1:n} KB", downloadedSize / 1000.0, fileSize / 1000.0);
              this.StatusPercentage = (int)(downloadedSize * 100.0 / fileSize);
              lastStatusUpdate = DateTime.Now;
            }
          }
        }
        reader.Close();
        response.Close();
      }
      catch (Exception e)
      {
        throw new Exception(string.Format("(2/3) Lonestar - {0}",e.Message));
      }
    }
    
    public void CommitToDatabase()
    {
      throw new NotImplementedException();
    }
  }
}
