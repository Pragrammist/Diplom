public interface IMarketPlaceRepository
{
    Task<MarketplaceIndetificator?> GetMarketPlace(string id);
    
    
    Task<bool> SetMarketplace(MarketplaceIndetificator marketplace);
    
    
    Task<bool> SetLoadedStatus(string id);

    
    Task<bool> SetCurrentUrl(string id, string url);

    
    Task AddCommentsToMarketPlace(CommentData[] comments);
    

    IAsyncEnumerable<CommentData> GetAllCommentsByUrl(string url);

    
    Task WriteCommentToCluster(string commentId, int clusterNumber, string url, int clustersCount, int iterator, string label);


    Task<CommentData> GetCommentById(string commentId);

    Task<IEnumerable<LabelInfo>> GetLabelsStats(string url);

    IAsyncEnumerable<CommentData> GetAllCommentFromCentroid(int centroidNumber, string url, int clustersCount, int iterator);


    Task<int> GetDimensions(string url);

    
    Task WriteComment(CommentData comment);


    Task<int> GetClustersCount(string url);


    
    
    Task WriteCentroidCoordinateSum(double distance, string url, int iterator, int clusterCount);



    Task<double> GetCentroidCoordinateSum(string url, int iterator, int clusterCount);



    Task<double> SaveCentroidCoordinateSumAsLast(string url, int iterator, int clusterCount);



    Task<double> GetLastCentroidCoordinateSum(string url, int clusterCount);


    int DefaultClusterCount { get; }


    Task<int> IncreaseGetClustersCount(string url);

    Task<ClusteringResult> CommentsAnalisResult(ClusteringResultFilter filter);

    Task SaveLastClusterIterationConfiguration(string url, int iterator, int clusterCounts);

    IAsyncEnumerable<CommentIterationData> IterateAllCommentsByClusters(string url);

    Task<LastClusteringIterationResult> GetLastClusterIterationConfiguration(string url);
    
    Task<IEnumerable<LabelInfo>> GetLabelsStatForClusters(string url, int clusterNumber);
    Task ClearCentroidCoordinateSum(string url, int iterator, int clusterCount);
}

