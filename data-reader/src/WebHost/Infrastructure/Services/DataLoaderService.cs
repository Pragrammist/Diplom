using System.Net.Mime;
using System.Linq;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.Playwright;



public class DataLoaderService
{
    readonly IMarketPlaceRepository _marketPlaceRepository;
    
    //HttpClient _yandexHttpClient;

    readonly IPage  _page;

    readonly ILogger<DataLoaderService> _logger;

    public DataLoaderService(
        IMarketPlaceRepository marketPlaceRepository, 
        ILogger<DataLoaderService> logger,
        IPage page)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _logger = logger;
        _page = page;
    }

    public async Task LoadData(string id)
    {
        var marketplace = await _marketPlaceRepository.GetMarketPlace(id);
        
        if(marketplace is null)
            return;

        
        if(string.IsNullOrEmpty(marketplace.CurrentUrl))
        {
            //await _page.GotoAsync(marketplace.Url);
            
                
            marketplace = marketplace with { 
                CurrentUrl = marketplace.Url
            };
        }
        

        



        
        
        
        
        

        _logger.LogInformation(@$"
                =====================================================================================================================
                    Marketplace URL: {marketplace.Url}
                =====================================================================================================================
                ");

        
        BackgroundJob.Enqueue<PageLoaderService>(s => s.LoadPage(marketplace));
    }

    


    

    async Task<string> GetScoreUrl()
    {
        string res = string.Empty;
        try{
            res = await _page.GetAttributeAsync("div.cENS_ a._1BsVs", "href") ?? string.Empty;
        }
        catch(TimeoutException){}
        
        return res;
        
    }
    
    





    
}

