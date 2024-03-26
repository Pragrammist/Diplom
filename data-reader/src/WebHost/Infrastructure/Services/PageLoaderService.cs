using System.Text;
using Hangfire;
using HtmlAgilityPack;




public class PageLoaderService
{

    
    
    //HttpClient _yandexHttpClient;
    
    
    readonly DuplexStreamLableService _duplexStreamLableService;

    readonly DuplexStreamVectorService _duplexStreamVectorService;

    readonly ILogger<DataLoaderService> _logger;
    
    readonly IMarketPlaceRepository _marketPlaceRepository;

    readonly Microsoft.Playwright.IPage _page;
    
    readonly IServiceProvider _serviceProvider;

    public PageLoaderService(
        DuplexStreamLableService duplexStreamLableService,
        DuplexStreamVectorService duplexStreamVectorService,
        IMarketPlaceRepository marketPlaceRepository,
        ILogger<DataLoaderService> logger,
        Microsoft.Playwright.IPage page,
        IServiceProvider serviceProvider
        )
    {
        _page = page;
        _duplexStreamLableService = duplexStreamLableService;
        _duplexStreamVectorService = duplexStreamVectorService;
        _logger = logger;
        _marketPlaceRepository = marketPlaceRepository;
        _serviceProvider = serviceProvider;
    }

    public async Task LoadPage(
        MarketplaceIndetificator marketplace)
    {
        
        if(string.IsNullOrEmpty(marketplace.CurrentUrl) || marketplace.LoadedUrl.Contains(marketplace.CurrentUrl))
        {
            _logger.LogInformation(@$"

=====================================================================================================================
                        Неудалось спарсить ссылку на отзывы
=====================================================================================================================

");
            return;
        }
            
        
        

        _logger.LogInformation(@$"

=====================================================================================================================
                        Marketplace Current Url: {marketplace.CurrentUrl}
=====================================================================================================================

");

        
            
        var isSet = await _marketPlaceRepository.SetMarketplace(marketplace);

        if(!isSet)
            throw new InvalidOperationException("cann't write current reading url in db");

        HtmlDocument scoreHtmlDoc = await GetScoreHtmlDocument(marketplace.CurrentUrl);
        _logger.LogInformation($"got current score page");

        try{
            var parsedComments = await GetParsedComments(scoreHtmlDoc, marketplace);

            

            _logger.LogInformation($"got current parsed comments");
            
            

            await LoadCommentsToDb(parsedComments);


            var nextUrl = await GetNextPage();
            
            
            
            await NextJob(marketplace, nextUrl);
            
            
        }
        catch{
            string text;

            if(scoreHtmlDoc.DocumentNode.InnerText.Length >= 100)
            {
                text = scoreHtmlDoc.DocumentNode.InnerText.Substring(0,100);
            }
            else 
                text = scoreHtmlDoc.DocumentNode.InnerText;
            _logger.LogError(@$"
            =====================================================================================================================
                DocumentStart: {text}
            =====================================================================================================================
            ");
            throw;
        }
    }


    async Task LoadCommentsToDb(CommentData[] commentData){
        await _marketPlaceRepository.AddCommentsToMarketPlace(commentData);
    }


    

    async Task NextJob(MarketplaceIndetificator marketplace, string nextUrl)
    {
    
        marketplace = marketplace with { 
            CurrentUrl = nextUrl
        };


    
        _logger.LogInformation($"================END OF PAGE================");
        
        if(!string.IsNullOrEmpty(marketplace.CurrentUrl))
            BackgroundJob.Enqueue<PageLoaderService>(s => s.LoadPage(marketplace));
        else
        {
            _logger.LogInformation($"================END OF LOADING PRODUCT================");
            var setLoaded = await _marketPlaceRepository.SetLoadedStatus(marketplace.Id);
            

            BackgroundJob.Enqueue<ClusterizationService>((s) => s.ClusterComments(marketplace.Url, 0));

            if(!setLoaded)
                throw new InvalidOperationException("cannot set loaded to redis db");
            

        }
        

    }

    async Task<HtmlDocument> GetScoreHtmlDocument(string currentUrl)
    {
        await _page.GotoAsync(currentUrl);
        var scoresPageText = await _page.ContentAsync();
        HtmlDocument scoreHtmlDoc = new HtmlDocument();
        scoreHtmlDoc.LoadHtml(scoresPageText);
        return scoreHtmlDoc;
    }

   


    async Task<string> GetNextPage()
    {
        string res = string.Empty;
        try{
            res = await _page.GetAttributeAsync("div._199Tg a._2prNU._3OFYT", "href") ?? string.Empty;
        } catch(TimeoutException){}
         

        

        return res;
    }

    async Task<CommentData[]> GetParsedComments(HtmlDocument scoreHtmlDoc, MarketplaceIndetificator marketplaceIndetificator)
    {
        var commentText = SelectCommentText(scoreHtmlDoc);
        var commentPlaceAndDate = SelectCommentDateAndPlace(scoreHtmlDoc);
        var internetPlaces = SelectInternetPlaces(scoreHtmlDoc);
        var sellers = SelectSellers(scoreHtmlDoc);
        var names = SelectInternetNames(scoreHtmlDoc);
        

        var parsedComments = new CommentData[commentPlaceAndDate.Count()];

        for(int i = 0; i < commentText.Length; i++)
        {
            var splitedPlaceAndData = (commentPlaceAndDate.ElementAtOrDefault(i) ?? "не удалось спарсить или не известно").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var date = splitedPlaceAndData.FirstOrDefault()?.Trim() ?? "не удалось спарсить или не известно";
            var place = splitedPlaceAndData.ElementAtOrDefault(1)?.Trim() ?? "не удалось спарсить или не известно";
            
            var label = await _duplexStreamLableService.SendText(commentText[i]);
            var vector = await _duplexStreamVectorService.SendText(commentText[i]);
            
            // var label = new Lable("category", 99);
            // var vector = new Vector(new []{1f, 2f, 3f});

            parsedComments[i] = new CommentData(
                commentText[i],
                date,
                place,
                internetPlaces.ElementAtOrDefault(i) ?? "не удалось спарсить или не известно",
                sellers.ElementAtOrDefault(i) ?? "не удалось спарсить или не известно",
                names.ElementAtOrDefault(i) ?? "не удалось спарсить или не известно",
                marketplaceIndetificator.MarketPlaceType,
                marketplaceIndetificator.Url,
                vector.Embeddings.ToArray(),
                label
            );
        }
        return parsedComments;
    }

    string[] SelectInternetPlaces(HtmlDocument scoreHtmlDoc)
    {
        
        var scoreNodes = scoreHtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'pcIgr')]//div[contains(@class, '_17xBD')]//span[contains(@class, '_1TQ6J')]")
        .Where((t,i) => (i + 1) % 2 == 1);
        var comments = new string[scoreNodes.Count()];
        int iComment = 0;
        foreach(var node in scoreNodes)
        {
            
            StringBuilder strNode = new StringBuilder();
            foreach(var child in node.ChildNodes)
            {
                strNode.AppendLine(child.InnerText);
            }
            var comment = strNode.ToString();
            
            comments[iComment] = comment.ClearFromTrashSymbols();
            iComment++;
        }
        return comments;
    }

    string[] SelectInternetNames(HtmlDocument scoreHtmlDoc)
    {
        var scoreNodes = scoreHtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, '_2o-om')]//div[contains(@class, '_1UL8e') and contains(@class, '_1mJcZ')]");
        var comments = new string[scoreNodes.Count()];
        int iComment = 0;
        foreach(var node in scoreNodes)
        {
            
            StringBuilder strNode = new StringBuilder();
            foreach(var child in node.ChildNodes)
            {
                strNode.AppendLine(child.InnerText);
            }
            var comment = strNode.ToString();
            
            comments[iComment] = comment;//.ClearFromTrashSymbols();
            iComment++;
        }
        return comments;
    }


    string[] SelectSellers(HtmlDocument scoreHtmlDoc)
    {
        var scoreNodes = scoreHtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'pcIgr')]//div[contains(@class, '_17xBD')]//span[contains(@class, '_1TQ6J')]")
        .Where((t,i) => (i + 1) % 2 == 0);
        var comments = new string[scoreNodes.Count()];
        int iComment = 0;
        foreach(var node in scoreNodes)
        {
            
            StringBuilder strNode = new StringBuilder();
            foreach(var child in node.ChildNodes)
            {
                strNode.AppendLine(child.InnerText);
            }
            var comment = strNode.ToString();
            
            comments[iComment] = comment.ClearFromTrashSymbols();
            iComment++;
        }
        return comments;
    }

    string[] SelectCommentText(HtmlDocument scoreHtmlDoc)
    {
        var scoreNodes = scoreHtmlDoc.DocumentNode.SelectNodes("//div[@class=\"_3IXcz\"]");
        var comments = new string[scoreNodes.Count];
        int iComment = 0;
        foreach(var node in scoreNodes)
        {
            
            StringBuilder strNode = new StringBuilder();
            foreach(var child in node.ChildNodes)
            {
                strNode.AppendLine(child.InnerText);
            }
            var comment = strNode.ToString();
            comments[iComment] = comment;
            iComment++;
        }
        return comments;
    }
    string[] SelectCommentDateAndPlace(HtmlDocument scoreHtmlDoc)
    {
        var scoreNodes = scoreHtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, '_3K8Ed')]//span[contains(@class, 'kx7am')]");
        var comments = new string[scoreNodes.Count];
        int iComment = 0;
        foreach(var node in scoreNodes)
        {
            
            StringBuilder strNode = new StringBuilder();
            foreach(var child in node.ChildNodes)
            {
                strNode.Append(child.InnerText);
            }
            var comment = strNode.ToString();
            comments[iComment] = comment.ClearFromTrashSymbols();
            iComment++;
        }
        return comments;
    }
}
