using System.Globalization;
using StackExchange.Redis;
using WebHost.Infrastructure.Services;

public class MarketPlaceRepository : IMarketPlaceRepository
{
    readonly IDatabase _redisDatabase;

    public int DefaultClusterCount => 1;

    public MarketPlaceRepository(IDatabase redisDatabase)
    {
        _redisDatabase = redisDatabase;
    }

    public async Task<MarketplaceIndetificator?> GetMarketPlace(string id)
    {
        string? redisValue = await _redisDatabase.StringGetAsync(id);
        
        return redisValue is not null 
            ? System.Text.Json.JsonSerializer.Deserialize<MarketplaceIndetificator>(redisValue)
            : null;
    }
    public async Task<bool> SetMarketplace(MarketplaceIndetificator marketplace)
    {   
        var json = System.Text.Json.JsonSerializer.Serialize(marketplace);

        var isAdded = await _redisDatabase.StringSetAsync(marketplace.Id, json);

        return isAdded;
    }

    public async Task<bool> SetLoadedStatus(string id)
    {   

        var marketplace = await GetMarketPlace(id);

        if(marketplace is null)
            return false;

        marketplace.IsLoaded = true;
        

        return await SetMarketplace(marketplace);
        
    }

    public async Task<bool> SetCurrentUrl(string id, string url)
    {
        var marketplace = await GetMarketPlace(id);

        if(marketplace is null)
            return false;

        marketplace = marketplace with { 
                CurrentUrl = url 
            };

        return await SetMarketplace(marketplace); 
    }


    public async Task AddCommentsToMarketPlace(CommentData[] comments)
    {
        foreach(var comment in comments){
            await WriteComment(comment);
        }
       
    }

    


    


    public async IAsyncEnumerable<CommentData> GetAllCommentsByUrl(string url)
    {
        await foreach(var commentId in _redisDatabase.SetScanAsync(url)){
            yield return await GetCommentById(commentId.ToString());
        }
    }

    public async Task<CommentData> GetCommentById(string commentId)
    {
        var jsonComment = await _redisDatabase.StringGetAsync(commentId.ToString());
            
        var commentResult = System.Text.Json.JsonSerializer.Deserialize<CommentData>(jsonComment.ToString()) ?? throw new Exception("comment cannot serelize from redis storage!");

        return commentResult;

    }

    public async IAsyncEnumerable<CommentData> GetAllCommentFromCentroid(int centroidNumber, string url, int clustersCount, int iterator)
    {
        var centroidId = CalcCentroidId(centroidNumber, url, clustersCount, iterator);

        await foreach(var commentId in _redisDatabase.SetScanAsync(centroidId)){
            yield return await GetCommentById(commentId.ToString());
        }
    }

    public async Task WriteCommentToCluster(string commentId, int clusterNumber, string url, int clustersCount, int iterator, string label)
    {
        var centroidId = CalcCentroidId(clusterNumber, url, clustersCount, iterator);
        
        await IncrOrStartCounter(CalcLabelDataClusterId(url, label, clusterNumber, iterator, clustersCount));
        await IncrOrStartCounter(CalcCommentCountForClusterId(url, clusterNumber, iterator, clustersCount));

        await SaveCommentsTags(commentId, () => CalcTagsForClusterId(url, clusterNumber, iterator, clustersCount));

        await SaveCommentsTags(commentId, () => CalcTagsForClusterLabelId(url, clusterNumber, iterator, clustersCount, label));
        
        await _redisDatabase.SetAddAsync(centroidId, commentId);   
    }

    

    public async Task<ClusteringResult> CommentsAnalisResult(ClusteringResultFilter filter)
    {
        List<CommentsCluster> clusters = new List<CommentsCluster>();
        var commentsResult = new List<CommentData>();

        int passedFilter = 0;
        await foreach(var cluster in IterateAllCommentsByClusters(filter.Url))
        {

            passedFilter = await AddCommentToCommentResult(commentsResult, cluster, filter, passedFilter);

            var clusterInfo = await GetClusterInfo(filter.Url, cluster.ClusterNumber);
            
            if (clusterInfo is not null)
                clusters.Add(clusterInfo);

        }

        var result = await ClusteringResult(filter.ProductName, filter.Url, clusters, commentsResult);
        return result;
    }


    

    async Task<int> AddCommentToCommentResult(
        List<CommentData> commentsResult, 
        CommentIterationData cluster, 
        ClusteringResultFilter filter,
        int passedFilter
    )
    {
        const int pageSize = 10;
        
        
        

        await foreach (var comment in cluster.Comments)
        {
            if (commentsResult.Count > pageSize)
                return passedFilter;

            var isPassedFilter = FilterComment(
                commentsResult,
                comment,
                filter,
                cluster,
                passedFilter,
                pageSize
            );
            if (isPassedFilter)
                passedFilter++;
        }
        return passedFilter;
    }


    async Task<ClusteringResult> ClusteringResult(string productName, string url, List<CommentsCluster> clusters, List<CommentData> comments)
    {
        var labelsInfo = await GetLabelsStats(url);
        var commentCount = await GetCommentCount(url);
        var tags = await GetTags(url);
        var result = new ClusteringResult(productName, url, clusters.ToArray(), labelsInfo.ToArray(), commentCount, comments.ToArray(), tags.ToArray());
        return result;
    }

    async Task<CommentsCluster?> GetClusterInfo(string url, int clusterNumber)
    {
        var clusterLabelsInfo = await GetLabelsStatForClusters(url, clusterNumber);


        if (clusterLabelsInfo.Count() == 0)
            return null;

        var commonLabel = clusterLabelsInfo.MaxBy(v => v.CommentCount)!.LabelName;

        var clusterCommentCount = await GetCommentCountForCluster(url, clusterNumber);
        var tags = await GetTags(url, clusterNumber);
        var clusterInfo = new CommentsCluster(commonLabel, clusterNumber, clusterLabelsInfo.ToArray(), clusterCommentCount, tags.ToArray());
        return clusterInfo;
    }

    bool FilterComment(
        List<CommentData> clusterComments,
        CommentData comment,
        ClusteringResultFilter filter,
        CommentIterationData cluster,
        int commentPassedFilter,
        int pageSize
    )
    {
        if (filter.ClusterNumber != cluster.ClusterNumber && filter.ClusterNumber != 0)
            return false;

        if (filter.Label != null && filter.Label != comment.Label.Label)
            return false;

        if (filter.Tag != null && comment.Text.SplitWordWithFilter().Count(str => str == filter.Tag) == 0)
            return false;

        


        var offset = (filter.Page - 1) * pageSize;
        var take = (filter.Page) * pageSize;

        

        if (commentPassedFilter >= offset && commentPassedFilter <= take)
        {
            clusterComments.Add(comment);
        }
        return true;
    }

    
    async Task SaveCommentsTags(string textOrCommentId, Func<string> IdExpression)
    {
        foreach(var word in textOrCommentId.SplitWordWithFilter())
        {
            await _redisDatabase.SortedSetIncrementAsync(IdExpression(), word, 1);
        }
    }


    public async Task SaveLastClusterIterationConfiguration(string url, int iterator, int clusterCounts)
    {
        var id = CalcConfigurationId(url);

        await _redisDatabase.StringSetAsync(id, $"{clusterCounts}:{iterator}");
    }

    public async Task<LastClusteringIterationResult> GetLastClusterIterationConfiguration(string url)
    {
        var id = CalcConfigurationId(url);

        var redisValue = await _redisDatabase.StringGetAsync(id);
        var data = redisValue.ToString().Split(":").Select(n => int.Parse(n)).ToArray();
        var result = new LastClusteringIterationResult(
            data[0],
            data[1]
        );
        return result;
    }

    public async IAsyncEnumerable<CommentIterationData> IterateAllCommentsByClusters(string url)
    {
        var lastConf = await GetLastClusterIterationConfiguration(url);

        for (int i = 1; i <= lastConf.ClustersCount; i++)
        {
            var asyncEnumerable = GetAllCommentFromCentroid(i, url, lastConf.ClustersCount, lastConf.Iterator);
            yield return new CommentIterationData(
                asyncEnumerable, 
            i);
        }
        

    }


    public async Task WriteCentroidCoordinateSum(double distance, string url, int iterator, int clusterCount)
    {
        var centroidId = CalcCentroidSumId(url, iterator, clusterCount);
        
        var cetroidDistValue = await _redisDatabase.StringGetAsync(centroidId);

        if(cetroidDistValue.IsNull)
            await _redisDatabase.StringSetAsync(centroidId, Math.Pow(distance,2));

        else{
            var strValue = cetroidDistValue.ToString();
            var distanceVal = double.Parse(strValue, CultureInfo.InvariantCulture);
            
            distanceVal += Math.Pow(distance,2);

            await _redisDatabase.StringSetAsync(centroidId, distanceVal);
        }
    }

    public async Task<double> SaveCentroidCoordinateSumAsLast(string url, int iterator, int clusterCount)
    {
        var coordinateSum = await GetCentroidCoordinateSum(url, iterator, clusterCount);
        var sumId = CalcCentroidLastSumId(url, clusterCount);
        await _redisDatabase.StringSetAsync(sumId, coordinateSum);
        

        return coordinateSum;
    }

    public async Task<double> GetLastCentroidCoordinateSum(string url, int clusterCount)
    {
        var sumId = CalcCentroidLastSumId(url, clusterCount);
        
        var coordinateSum = await _redisDatabase.StringGetAsync(sumId);

        return double.Parse(coordinateSum.ToString(), CultureInfo.InvariantCulture);
    }
    

    public async Task<double> GetCentroidCoordinateSum(string url, int iterator, int clusterCount)
    {
        var centroidId = CalcCentroidSumId(url, iterator, clusterCount);

        var cetroidDistValue = await _redisDatabase.StringGetAsync(centroidId);

        return double.Parse(cetroidDistValue.ToString(), CultureInfo.InvariantCulture);;
    }


    

    

    public async Task<int> GetDimensions(string url)
    {
        var result = await _redisDatabase.StringGetAsync(CalcDemensionsId(url));
        
        return int.Parse(result!);
    }
    
    public async Task WriteComment(CommentData comment)
    {
        
        var commentJson = System.Text.Json.JsonSerializer.Serialize(comment);
        await _redisDatabase.StringSetAsync(comment.Id, commentJson);
        await _redisDatabase.SetAddAsync(comment.Url, comment.Id);
        await _redisDatabase.SetAddAsync(comment.Label.Label, comment.Id);
        await _redisDatabase.StringSetAsync(CalcDemensionsId(comment.Url), comment.Embeddings.Length);
        
        await IncrOrStartCounter(CalcLabelDataId(comment.Url, comment.Label.Label));
        await IncrOrStartCounter(CalcCommentCountId(comment.Url));

        await _redisDatabase.SetAddAsync(CalcCommentsLabelsId(comment.Url), comment.Label.Label);

        await SaveCommentsTags(comment.Text, () => CalcUrlTagsId(comment.Url));

        await SaveCommentsTags(comment.Text, () => CalcLabelTagsId(comment.Url, comment.Label.Label));

    }

    

    
    async Task<IEnumerable<TagData>> GetTags(Func<string> IdExpression)
    {
        const int maxTags = 40;
        var id = IdExpression();
        var redisResult = await _redisDatabase.SortedSetRangeByScoreWithScoresAsync(id, skip:0, take:maxTags, order: Order.Descending);
        
        var result = Array.ConvertAll(redisResult, v => new TagData (v.Element.ToString(), v.Score));
        return result;


    }

    public async Task<IEnumerable<TagData>> GetTags(string url) => await GetTags(() => CalcUrlTagsId(url));

    
    public async Task<IEnumerable<TagData>> GetTags(string url, string label) => await GetTags(() => CalcLabelTagsId(url, label));

    
    public async Task<IEnumerable<TagData>> GetTags(string url, int clusterNumber)
    {
        var lastConf = await GetLastClusterIterationConfiguration(url);

        return await GetTags(() => CalcTagsForClusterId(url, clusterNumber, lastConf.Iterator, lastConf.ClustersCount));
    }
        

    
    public async Task<IEnumerable<TagData>> GetTags(string url, int clusterNumber, string label)
    {
        var lastConf = await GetLastClusterIterationConfiguration(url);

        return await GetTags(() => CalcTagsForClusterLabelId(url, clusterNumber, lastConf.Iterator, lastConf.ClustersCount, label));
    }




    async Task<int> GetCommentCount(string url)
    {

        return int.Parse((await _redisDatabase.StringGetAsync(CalcCommentCountId(url))).ToString());

        

    }

    async Task<int> GetCommentCountForCluster(string url, int clusterNumber)
    {
        var lastConf = await GetLastClusterIterationConfiguration(url);
        
        return int.Parse((await _redisDatabase.StringGetAsync(CalcCommentCountForClusterId(url, clusterNumber, lastConf.Iterator, lastConf.ClustersCount))).ToString());
    }

    public async Task<IEnumerable<LabelInfo>> GetLabelsStats(string url)
    {
        //
        return await GetLabelsStats(
            () => CalcCommentsLabelsId(url),
            label => CalcLabelDataId(url, label),
            () => CalcCommentCountId(url),
            (label) => CalcLabelTagsId(url, label)
        );
    }

    public async Task<IEnumerable<LabelInfo>> GetLabelsStatForClusters(string url, int clusterNumber)
    {
        var lastConf = await GetLastClusterIterationConfiguration(url);
        return await GetLabelsStats(
            () => CalcCommentsLabelsId(url),
            label => CalcLabelDataClusterId(url, label, clusterNumber, lastConf.Iterator, lastConf.ClustersCount),
            () => CalcCommentCountForClusterId(url, clusterNumber, lastConf.Iterator, lastConf.ClustersCount),
            (label) => CalcTagsForClusterLabelId(url, clusterNumber, lastConf.Iterator, lastConf.ClustersCount, label)
        );
    }

    async Task<IEnumerable<LabelInfo>> GetLabelsStats(
        Func<string> CommentsLabelsIdExpression, 
        Func<string, string> CommentCountForLabelIdExpression, 
        Func<string> CommentCountIdExpression,
        Func<string, string> GetTagsExprId)
    {
        var result = new List<LabelInfo>();

        var commentCounts = await GetAndParseValue(CommentCountIdExpression());

        if (commentCounts == 0)
            return result;

        await foreach (string? redisValue in _redisDatabase.SetScanAsync(CommentsLabelsIdExpression()))
        {
            var label = redisValue!.ToString();
            var commentsCountForLabel = await GetAndParseValue(CommentCountForLabelIdExpression(label));
            var tags = await GetTags(() => GetTagsExprId(label));
            var labelInfo = new LabelInfo(label, Math.Round((commentsCountForLabel / (double)commentCounts) * 100, 1), commentsCountForLabel, tags.ToArray());
            result.Add(labelInfo);
        }

        return result;

    }

    async Task<int> GetAndParseValue(string id)
    {
        string? redisValue = await _redisDatabase.StringGetAsync(id);
        var result = int.Parse(redisValue ?? "0");
        return result;
    }

    async Task IncrOrStartCounter(string id)
    {
        var value = await _redisDatabase.StringGetAsync(id);

        var intValue = 0;
        
        if (value.HasValue)
            intValue = int.Parse(value.ToString());
        
        intValue++;
        
        await _redisDatabase.StringSetAsync(id, intValue);
    }


    public async Task<int> GetClustersCount(string url)
    {
        var clusterRedisValue = await _redisDatabase.StringGetAsync(CalcClustersCountId(url));

        if(!clusterRedisValue.IsNullOrEmpty)
            return int.Parse(clusterRedisValue.ToString());


        await _redisDatabase.StringSetAsync(CalcClustersCountId(url), DefaultClusterCount);

        return DefaultClusterCount;
    }

    public async Task<int> IncreaseGetClustersCount(string url)
    {
        var clusterRedisValue = await _redisDatabase.StringGetAsync(CalcClustersCountId(url));

        var clusterCount = int.Parse(clusterRedisValue.ToString());
        
        clusterCount++;

        await _redisDatabase.StringSetAsync(CalcClustersCountId(url), clusterCount);

        return clusterCount;
    }

    string CalcCentroidId (int centroidNumber, string url, int clustersCount, int iterator) => $"{centroidNumber}:{url}:{clustersCount}:${iterator}";

    string CalcCentroidSumId (string url, int iterator, int clusterCount) => $"{url}:{iterator}:{clusterCount}:sum";

    string CalcCentroidLastSumId (string url, int clusterCount) => $"{url}:{clusterCount}:sum:last";

    string CalcDemensionsId (string url) => $"demensions:{url}";

    string CalcConfigurationId(string url) => $"last-conf:{url}";

    string CalcClustersCountId (string url) => $"clusters:{url}";

    string CalcLabelDataId(string url, string label) => $"$label-data:{url}:{label}";

    string CalcCommentCountId(string url) => $"comment-count:{url}";

    string CalcCommentsLabelsId(string url) => $"comments-labels:${url}";

    string CalcCommentCountForClusterId(string url, int clusterNumber, int iterator, int clustersCount) => $"label-comment-count::{url}:{clusterNumber}: {iterator}: {clustersCount}";

    string CalcLabelDataClusterId(string url, string label, int clusterNumber, int iterator, int clustersCount) => $"$label-data:{url}:{label}:{clusterNumber}: {iterator}: {clustersCount}";

    string CalcUrlTagsId(string url) => $"product-tags:{url}";
    
    string CalcLabelTagsId(string url, string label) => $"product-tags:{url}:{label}";

    string CalcTagsForClusterId(string url, int clusterNumber, int iterator, int clustersCount) => $"product-tags:{url}:{clusterNumber}:{iterator}:{clustersCount}";

    string CalcTagsForClusterLabelId(string url, int clusterNumber, int iterator, int clustersCount, string label) => $"product-tags:{url}:{clusterNumber}:{iterator}:{clustersCount}:{label}";
}

public record ClusteringResult(
    string ProductName,
    string ProductUrl,
    CommentsCluster[] Clusters,
    LabelInfo[] LabelsInfo,
    int CommentCount,
    CommentData[] Comments,
    TagData[] Tags
)
{

};


public record TagData(string Tag, double Score);

public record ClusteringResultFilter (
    string Url, 
    string ProductName,
    string? Tag = null,
    string? searchStr = null,
    string? Label = null,
    int ClusterNumber = 0,
    int Page = 1
);

public record LastClusteringIterationResult(int ClustersCount, int Iterator);


public record CommentIterationData(IAsyncEnumerable<CommentData> Comments, int ClusterNumber);

public record CommentsCluster(
    string CommonLabel,
    int ClusterNumber,
    LabelInfo[] labelsInfo,
    int CommentCount,
    TagData[] Tags
);

public record LabelInfo(string LabelName, double Percent, int CommentCount, TagData[] Tags);

