using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketplaceWebServiceProducts;
using System.Threading;

namespace Cheapees
{
  public class AmazonListingDataViewModel : UpdatableViewModelBase
  {

    public AmazonListingDataViewModel()
    {
      this.Title = "Listing Data - Amazon (SalesRank, BuyBox)";
      this.Description = "Updates SalesRank and BuyBox data for all ASINs in database.";
      this._uiUpdateThreshold = new TimeSpan(0, 0, 0, 1, 0);
      this.ServiceId = "AmazonListingData";
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

          List<AsinProductData> asinData = DownloadListingData(asins);

          CommitToDatabase(asinData);

          //Status
          this.Status = UpdatableStatus.Ok;
          this.StatusDescription = "All Amazon listings retrieved successfully.";
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

    private void CommitToDatabase(List<AsinProductData> asinData)
    {
      using (var db = new CheapeesEntities())
      {
        foreach (var a in asinData)
        {
          AmazonListing dbEntry = new AmazonListing();
          dbEntry.Asin = a.Asin;
          dbEntry.BuyBox = a.BuyBoxTotalPrice;
          dbEntry.CurrentlyOwnBuyBox = a.CurrentlyOwnBuyBox;
          dbEntry.Date = DateTime.Now;
          dbEntry.SalesRank = a.SalesRankTopLevel;
          
          if (db.AmazonListings.Where(o => o.Asin.Equals(dbEntry.Asin) && o.Date.Equals(dbEntry.Date)).Count() == 0 && db.AmazonListings.Local.Where(o => o.Asin.Equals(dbEntry.Asin) && o.Date.Equals(dbEntry.Date)).Count() == 0)
          {
            db.AmazonListings.Add(dbEntry);
          }
        }

        db.SaveChanges();
      }
    }

    private List<string> GetAsinList()
    {
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

    private List<AsinProductData> DownloadListingData(List<string> asins)
    {
      List<AsinProductData> asinData = new List<AsinProductData>();
      asins = asins.Distinct().ToList(); // There cannot be duplicate ASINs in request

      for (int i = 0; i < asins.Count; i += 20) //Each request is limited to 20 ASINs at a time
      {
        this.StatusDescription = string.Format("Downloading ASIN product details - {0:0.00}% ({1}/{2})", (i * 100.0) / asins.Count, i, asins.Count);
        this.StatusPercentage = (i * 100) / asins.Count;
        List<string> requestedAsins = new List<string>();
        requestedAsins = asins.GetRange(i, Math.Min(20, asins.Count - i)).Distinct().ToList();


        List<AsinProductData> responseData = GetDataFromMws(requestedAsins);
        asinData.AddRange(responseData);

        Thread.Sleep(1000); // Throttle requests so we will make requests more consistently
      }

      return asinData;
  }

    private List<AsinProductData> GetDataFromMws(List<string> requestedAsins)
    {
      List<AsinProductData> asinData = new List<AsinProductData>();

      string accessKey = System.Configuration.ConfigurationManager.AppSettings["MwsAccK"];
      string secretKey = System.Configuration.ConfigurationManager.AppSettings["MwsSecK"];
      string sellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
      string marketplaceId = System.Configuration.ConfigurationManager.AppSettings["MwsMktpl"];

      MarketplaceWebServiceProductsConfig config = new MarketplaceWebServiceProductsConfig();
      config.ServiceURL = "https://mws.amazonservices.com ";
      config.SetUserAgentHeader("CMA", "1.0", "C#", new string[] { });
      MarketplaceWebServiceProductsClient client = new MarketplaceWebServiceProductsClient(accessKey, secretKey, config);

      MarketplaceWebServiceProducts.Model.GetCompetitivePricingForASINRequest request = new MarketplaceWebServiceProducts.Model.GetCompetitivePricingForASINRequest();
      request.ASINList = new MarketplaceWebServiceProducts.Model.ASINListType();
      request.ASINList.ASIN = requestedAsins;
      request.SellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
      request.MarketplaceId = System.Configuration.ConfigurationManager.AppSettings["MwsMktpl"];


      MarketplaceWebServiceProducts.Model.GetCompetitivePricingForASINResponse response = new MarketplaceWebServiceProducts.Model.GetCompetitivePricingForASINResponse();


      try { response = client.GetCompetitivePricingForASIN(request); } catch {}
      while (response.ResponseHeaderMetadata == null || response.ResponseHeaderMetadata.QuotaRemaining < 1000)
      {
        if (response.ResponseHeaderMetadata == null)
        {
          this.StatusDescription = string.Format("No response given to MWS Request");
        }
        else
          this.StatusDescription = string.Format("API Quota reached. Remaining: {0} of {1}, Resets: {2},", response.ResponseHeaderMetadata.QuotaRemaining, response.ResponseHeaderMetadata.QuotaMax, response.ResponseHeaderMetadata.QuotaResetsAt.Value.ToShortTimeString());

        Thread.Sleep(20000);
        try { response = client.GetCompetitivePricingForASIN(request); } catch {}
      }

      foreach (var result in response.GetCompetitivePricingForASINResult)
      {
        AsinProductData asinDataItem = new AsinProductData();
        asinDataItem.Asin = result.ASIN;
        if (result.IsSetProduct())
        {
          if (result.Product.IsSetSalesRankings())
          {
            foreach (var salesRank in result.Product.SalesRankings.SalesRank)
            {
              if (salesRank.ProductCategoryId.Contains("display"))
              {
                asinDataItem.SalesRankTopLevel = (int)salesRank.Rank;
                asinDataItem.SalesRankTopLevelCategory = salesRank.ProductCategoryId;
              }
              else if (!asinDataItem.SalesRankSecondary.ContainsKey(salesRank.ProductCategoryId))
              {
                asinDataItem.SalesRankSecondary.Add(salesRank.ProductCategoryId, (int)salesRank.Rank);
              }
            }
          }
          if (result.Product.IsSetCompetitivePricing())
          {
            foreach (var price in result.Product.CompetitivePricing.CompetitivePrices.CompetitivePrice)
            {
              if (price.CompetitivePriceId.Equals("1"))
              {
                asinDataItem.CurrentlyOwnBuyBox = price.belongsToRequester;
                if (price.IsSetPrice() && price.Price.IsSetLandedPrice())
                  asinDataItem.BuyBoxTotalPrice = price.Price.LandedPrice.Amount;
              }
            }
          }
        }
        
        asinData.Add(asinDataItem);
      }

      return asinData;
    }
  }

    public class AsinProductData
  {
    public string Asin { get; set; }
    public Nullable<int> SalesRankTopLevel { get; set; }
    public string SalesRankTopLevelCategory { get; set; }
    public Dictionary<string, int> SalesRankSecondary { get; set; }
    public bool CurrentlyOwnBuyBox { get; set; }
    public Nullable<decimal> BuyBoxTotalPrice { get; set; }

    public AsinProductData()
    {
      SalesRankSecondary = new Dictionary<string, int>();
    }
  }
}
