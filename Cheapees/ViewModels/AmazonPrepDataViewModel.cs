using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FBAInboundServiceMWS;
using System.Threading;

namespace Cheapees
{
  public class FbaPrepDataViewModel : UpdatableViewModelBase
  {

    public FbaPrepDataViewModel()
    {
      this.Title = "FBA - Prep Instructions";
      this.Description = "Updates required prep instructions per ASIN.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "FbaPrepData";
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

          //Start Updating
          List<string> asins = GetAsinList();

          List<AsinPrepData> data = DownloadPrepData(asins);

          CommitToDatabase(data);

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "All Amazon prep instructions retrieved successfully.";
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

    private void CommitToDatabase(List<AsinPrepData> asinData)
    {
      using (var db = new CheapeesEntities())
      {
        db.Configuration.AutoDetectChangesEnabled = false;
        db.Configuration.ValidateOnSaveEnabled = false;

        for (int i = 0; i < asinData.Count; i++)
        {
          var a = asinData[i];
          if (db.AmazonPrepInstructions.Where(o => o.Asin.Equals(a.Asin)).Count() == 0 && db.AmazonPrepInstructions.Local.Where(o => o.Asin.Equals(a.Asin)).Count() == 0)
          {
            AmazonPrepInstruction dbEntry = new AmazonPrepInstruction();
            dbEntry.Asin = a.Asin;
            dbEntry.Labelling = a.Labelling;
            dbEntry.PrepRequired = a.Prep;

            db.AmazonPrepInstructions.Add(dbEntry);
          }
          else
          {
            AmazonPrepInstruction dbEntry = db.AmazonPrepInstructions.Where(o => o.Asin.Equals(a.Asin)).SingleOrDefault();
            if (dbEntry != null)
            {
              dbEntry.Labelling = a.Labelling;
              dbEntry.PrepRequired = a.Prep;
            }
          }

          if (i % 1000 == 0)
          {
            this.StatusDescription = string.Format("Committing to database - {0:0.00}% ({1}/{2})", i * 100.0 / asinData.Count, i, asinData.Count);
            this.StatusPercentage = i * 100 / asinData.Count;
            db.SaveChanges();
          }

        }

        db.SaveChanges();
      }
    }

    private List<string> GetAsinList()
    {
      this.StatusDescription = string.Format("Getting local ASIN list");
      List<string> asins = new List<string>();
      List<Inventory> listings = new List<Inventory>();
      using (var db = new CheapeesEntities())
      {
        listings = db.Inventories.Where(o => o.Asin != null).ToList();
      }

      foreach (var listing in listings)
      {
        if (listing.Asin != null && listing.Asin.Length != 0)
          asins.Add(listing.Asin);
      }

      return asins;
    }

    private List<AsinPrepData> DownloadPrepData(List<string> asins)
    {
      List<AsinPrepData> asinData = new List<AsinPrepData>();
      asins = asins.Distinct().ToList(); // There cannot be duplicate ASINs in request

      for (int i = 0; i < asins.Count; i += 50) //Each request is limited to 50 ASINs at a time
      {
        this.StatusDescription = string.Format("Downloading FBA prep instructions for ASINs - {0:0.00}% ({1}/{2})", (i * 100.0) / asins.Count, i, asins.Count);
        this.StatusPercentage = (i * 100) / asins.Count;
        List<string> requestedAsins = new List<string>();
        requestedAsins = asins.GetRange(i, Math.Min(50, asins.Count - i)).Distinct().ToList();


        List<AsinPrepData> responseData = GetDataFromMws(requestedAsins);
        asinData.AddRange(responseData);

        Thread.Sleep(500); // Throttle requests so we will make requests more consistently
      }

      this.StatusPercentage = 100;

      return asinData;
  }

    private List<AsinPrepData> GetDataFromMws(List<string> requestedAsins)
    {
      List<AsinPrepData> asinData = new List<AsinPrepData>();

      string accessKey = System.Configuration.ConfigurationManager.AppSettings["MwsAccK"];
      string secretKey = System.Configuration.ConfigurationManager.AppSettings["MwsSecK"];
      string sellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
      string marketplaceId = System.Configuration.ConfigurationManager.AppSettings["MwsMktpl"];

      FBAInboundServiceMWSConfig config = new FBAInboundServiceMWSConfig();
      config.ServiceURL = "https://mws.amazonservices.com ";
      config.SetUserAgentHeader("CMA", "1.0", "C#", new string[] { });
      FBAInboundServiceMWSClient client = new FBAInboundServiceMWSClient(accessKey, secretKey, config);

      FBAInboundServiceMWS.Model.GetPrepInstructionsForASINRequest request = new FBAInboundServiceMWS.Model.GetPrepInstructionsForASINRequest();
      request.AsinList = new FBAInboundServiceMWS.Model.AsinList();
      request.AsinList.Id = requestedAsins;
      request.SellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
      request.ShipToCountryCode = "US";

      FBAInboundServiceMWS.Model.GetPrepInstructionsForASINResponse response = new FBAInboundServiceMWS.Model.GetPrepInstructionsForASINResponse();


      try { response = client.GetPrepInstructionsForASIN(request); } catch (Exception e) {}
      while (response.ResponseHeaderMetadata == null || response.ResponseHeaderMetadata.QuotaRemaining < 1000)
      {
        if (response.ResponseHeaderMetadata == null)
        {
          this.StatusDescription = string.Format("No response given to MWS Request");
        }
        else
          this.StatusDescription = string.Format("API Quota reached. Remaining: {0} of {1}, Resets: {2},", response.ResponseHeaderMetadata.QuotaRemaining, response.ResponseHeaderMetadata.QuotaMax, response.ResponseHeaderMetadata.QuotaResetsAt.Value.ToShortTimeString());

        Thread.Sleep(20000);
        try { response = client.GetPrepInstructionsForASIN(request); } catch (Exception e) { }
      }

      foreach (var result in response.GetPrepInstructionsForASINResult.ASINPrepInstructionsList.ASINPrepInstructions)
      {
        if (result.IsSetASIN())
        {
          AsinPrepData asinPrep = new AsinPrepData();
          asinPrep.Asin = result.ASIN;

          if (result.IsSetBarcodeInstruction())
          {
            if (result.BarcodeInstruction.Equals("RequiresFNSKULabel"))
              asinPrep.Labelling = "Required";
            else if (result.BarcodeInstruction.Equals("MustProvideSellerSKU"))
              asinPrep.Labelling = "Undetermined";
          }

          if (result.IsSetPrepGuidance())
          {
            if (result.PrepGuidance.Equals("SeePrepInstructionsList"))
            {
              // prep exists
              if (result.IsSetPrepInstructionList())
              {
                foreach (var prep in result.PrepInstructionList.PrepInstruction)
                {
                  asinPrep.Prep += prep;
                }
              }
            }
            else if (result.PrepGuidance.Equals("NoAdditionalPrepRequired"))
            {
              asinPrep.Prep = "None";
            }
            else if (result.PrepGuidance.Equals("ConsultHelpDocuments"))
            {
              asinPrep.Prep = "Undetermined";
            }

          }

          asinData.Add(asinPrep);
        }
        
        
      }

      return asinData;
    }

  }

  public class AsinPrepData
  {
    public string Asin;
    public string Labelling;
    public string Prep;
  }
}
