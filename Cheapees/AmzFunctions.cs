using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketplaceWebServiceProducts;
using System.Threading;
using System.IO;

namespace Cheapees
{
  class AmzFunctions
  {

    public List<AmazonCategoryList> GetProductCategories(List<string> requestedAsins)
    {
      List<AmazonCategoryList> asinData = new List<AmazonCategoryList>();

      string accessKey = System.Configuration.ConfigurationManager.AppSettings["MwsAccK"];
      string secretKey = System.Configuration.ConfigurationManager.AppSettings["MwsSecK"];
      string sellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
      string marketplaceId = System.Configuration.ConfigurationManager.AppSettings["MwsMktpl"];

      MarketplaceWebServiceProductsConfig config = new MarketplaceWebServiceProductsConfig();
      config.ServiceURL = "https://mws.amazonservices.com ";
      config.SetUserAgentHeader("CMA", "1.0", "C#", new string[] { });
      MarketplaceWebServiceProductsClient client = new MarketplaceWebServiceProductsClient(accessKey, secretKey, config);

      requestedAsins = requestedAsins.Distinct().ToList();
      StringBuilder csv = new StringBuilder();
      csv.Append(string.Format("{0},{1},{2}{3}", "ASIN", "CatID", "CatName", Environment.NewLine));

      for (int i = 0; i < requestedAsins.Count; i++)
      {
        string asin = requestedAsins[i];

        Console.WriteLine("{0}/{1} ({2:0.00}%)", i, requestedAsins.Count, i * 100.0 / requestedAsins.Count);

        MarketplaceWebServiceProducts.Model.GetProductCategoriesForASINRequest request = new MarketplaceWebServiceProducts.Model.GetProductCategoriesForASINRequest();
        request.ASIN = asin;
        request.SellerId = System.Configuration.ConfigurationManager.AppSettings["MwsSeller"];
        request.MarketplaceId = System.Configuration.ConfigurationManager.AppSettings["MwsMktpl"];


        MarketplaceWebServiceProducts.Model.GetProductCategoriesForASINResponse response = new MarketplaceWebServiceProducts.Model.GetProductCategoriesForASINResponse();


        try { response = client.GetProductCategoriesForASIN(request); } catch { }
        while (response.ResponseHeaderMetadata == null || response.ResponseHeaderMetadata.QuotaRemaining < 10)
        {
          if (response.ResponseHeaderMetadata == null)
          {
            Console.WriteLine(string.Format("No response given to MWS Request"));
            break;
          }
          else
            Console.WriteLine(string.Format("API Quota reached. Remaining: {0} of {1}, Resets: {2},", response.ResponseHeaderMetadata.QuotaRemaining, response.ResponseHeaderMetadata.QuotaMax, response.ResponseHeaderMetadata.QuotaResetsAt.Value.ToShortTimeString()));

          Thread.Sleep(30000);
          try { response = client.GetProductCategoriesForASIN(request); } catch { }
        }

        if (response != null && response.IsSetGetProductCategoriesForASINResult())
        {
          if (response.GetProductCategoriesForASINResult.IsSetSelf())
          {
            AmazonCategoryList asinDeets = new AmazonCategoryList();
            asinDeets.Asin = asin;
            foreach (var cat in response.GetProductCategoriesForASINResult.Self)
            {
            var newLine = string.Format("{0},{1},{2}{3}", asin, cat.ProductCategoryId, cat.ProductCategoryName, Environment.NewLine);
            csv.Append(newLine);
            }
          }
        }

        Thread.Sleep(5000);
      }

      try {
        File.WriteAllText("AmazonCategories.csv", csv.ToString());
      } catch (Exception e)
      {

      }

      Console.WriteLine("CHECK THE NEXT PART IF IT FAILED.");

      return asinData;
    }

    public List<string> GetAsinList()
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
  }

  public class AmazonCategoryList
  {
    public string Asin;
    public List<AmzCat> Categories;

    public AmazonCategoryList()
    {
      Categories = new List<AmzCat>();
    }
  }

  public class AmzCat
  {
    public string CategoryId;
    public string CategoryName;
  }
}
