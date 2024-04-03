using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebHost.Hubs;


public class ClusterizationService
{


    readonly IMarketPlaceRepository _marketPlaceRepository;
    
    readonly ILogger<DataLoaderService> _logger;

    readonly IServiceProvider _serviceProvider;
    public ClusterizationService(IMarketPlaceRepository marketPlaceRepository, ILogger<DataLoaderService> logger, IServiceProvider serviceProvider)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ClusterComments(string url, int iterator = 0)
    {
        int clusterCount = await _marketPlaceRepository.GetClustersCount(url);
        if(iterator == 0)
        {
            _logger.LogInformation($"Начало кластеризации с {clusterCount} количеством кластеров");
        }
        _logger.LogInformation($"Начало итерации {iterator}");

        int dimensions = await _marketPlaceRepository.GetDimensions(url);

        

        var centroids = await GetCentroids(clusterCount, dimensions, url, iterator);

        await _marketPlaceRepository.ClearCentroidCoordinateSum(url, iterator, clusterCount);

        await foreach(var comment in _marketPlaceRepository.GetAllCommentsByUrl(url))
        {
            var p = comment.Embeddings;

            var (nearestCentroid, minDistance) =  GetNumberNearestCentroid(centroids, p);

            await _marketPlaceRepository.WriteCommentToCluster(comment.Id, nearestCentroid, url, clusterCount, iterator, comment.Label.Label);

            await _marketPlaceRepository.WriteCentroidCoordinateSum(minDistance, url, iterator, clusterCount);
        }
        
        var avgDifference = 0d;
        
        if(iterator != 0)
            avgDifference = await GetDifference(url, iterator, clusterCount);
        
        _logger.LogInformation($"Конец итерации {iterator}");
        if(iterator == 0 || DifferanceIsLarge(avgDifference))
        {
            iterator++;
            BackgroundJob.Enqueue<ClusterizationService>((s) => s.ClusterComments(url, iterator));
            return;
        }
        
        await StartNewIterationsLogic(iterator, url, clusterCount);
    }

    async Task StartNewIterationsLogic(int iterator, string url, int clusterCount)
    {
        _logger.LogInformation($"Конец кластеризации с {clusterCount} количеством кластеров");

        var lastSum = await _marketPlaceRepository.SaveCentroidCoordinateSumAsLast(url, iterator, clusterCount);

        var savedIteratorForEnd = iterator;

        iterator = 0;

        
        
        if(clusterCount < _marketPlaceRepository.DefaultClusterCount + 2)
        {
            await StartNewIterations(url, iterator);
            return;
        }
        var prevSum = await _marketPlaceRepository.GetLastCentroidCoordinateSum(url, clusterCount - 1);
        var prevPrevSum = await _marketPlaceRepository.GetLastCentroidCoordinateSum(url, clusterCount - 2);
        var prevLastDif = prevSum - lastSum;
        var prevAndPrevPrevDif = prevSum - prevPrevSum;
        if(DifferanceIsLarge(prevLastDif) || DifferanceIsLarge(prevAndPrevPrevDif))
            await StartNewIterations(url, iterator);
        else
        {
            await _marketPlaceRepository.SaveLastClusterIterationConfiguration(url, savedIteratorForEnd, clusterCount);
            await EndLoading(url);
            
            _logger.LogInformation($"Конец кластеризации");
        }
    }

    async Task EndLoading(string url)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var product = await UpdateProductStatus(url, scope);

        await NotfiyIsLoaded(product.ProductName, scope);

        

    }
    async Task<Product> UpdateProductStatus(string url, IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await dbContext.Products.FirstAsync(p => p.Url == url);
        product.IsLoaded = true;
        await dbContext.SaveChangesAsync();
        return product;
    }

    async Task NotfiyIsLoaded(string productName, IServiceScope scope)
    {
        var productsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ProductsHub>>();

        await productsHub.Clients.All.SendAsync("SetIsLoaded", productName);

        
    }

    async Task StartNewIterations(string url, int iterator)
    {
        await _marketPlaceRepository.IncreaseGetClustersCount(url);
        BackgroundJob.Enqueue<ClusterizationService>((s) => s.ClusterComments(url, iterator));
    }

    bool DifferanceIsLarge(double difference)
    {
        return Math.Abs(difference) > 0.05;
    }

    async Task<double> GetDifference(string url, int iterator, int clusterCount)
    {
        var currentSum = await _marketPlaceRepository.GetCentroidCoordinateSum(url, iterator, clusterCount);
        var prevSum = await _marketPlaceRepository.GetCentroidCoordinateSum(url, iterator-1, clusterCount);
        return Math.Pow(currentSum - prevSum, 2);
    }



    double GetDistance(float[] p1, float[] p2){
        double sum = 0;

        foreach(var negenation in GetDistanceNegetion(p1, p2))
            sum += negenation;
        
        return Math.Sqrt(sum);
    }

    (int, double) GetNumberNearestCentroid(IEnumerable<float[]> centroids, float[] p)
    {
        var minDistance = double.MaxValue;
        var minDistanceClusterCount = 0;
        var i = 1;
        foreach(var centroid in centroids)
        {
            var distance = GetDistance(centroid, p);
            if(distance < minDistance)
            {
                minDistance = distance;
                minDistanceClusterCount = i;
            }

            i++;
        }
        return (minDistanceClusterCount, minDistance);

       
    }


    IEnumerable<double> GetDistanceNegetion(float[] p1, float[] p2)
    {
        if(p1.Length != p2.Length)
            throw new Exception("РАЗНЫЕ МАССИВЫ ДЛЯ ИЗМЕРЕНИЯ ДИСТАНЦИИ В АЛГОРИТМЕ КЛАСТЕРИЗАЦИИ");

        for(int i = 0; i < p1.Length; i++){
            var coordinate1 = p1[i];
            var coordinate2 = p2[i];
            var negenation = coordinate1 - coordinate2;
            
            yield return Math.Pow(negenation, 2d);
        }
    }

    

    float GetRandomCentroid(int clusterCount, int clusterNumber){
        return 1f / clusterCount * clusterNumber;
    }

  
    async Task<IEnumerable<float[]>> GetCentroids(int clusterCount, int dimensions, string url, int iterator)
    {
        var result = new LinkedList<float[]>();
        if(iterator == 0)
        {
            foreach (var centroid in GetRandomCentroids(clusterCount, dimensions))
            {
                result.AddLast(centroid);
            }
            return result;
        }
        
        await foreach(var centroid in CalculateCentroids(url, iterator-1))
        {
            result.AddLast(centroid);
        }

        return result;
        
        
    }

   



    IEnumerable<float[]> GetRandomCentroids(int clusterCount, int dimensions){
        
        for(int i = 1; i <= clusterCount; i++)
        {
            var oneCentroidValue = GetRandomCentroid(clusterCount, i);
            var floatCentroids = new float[dimensions];
            
            Array.Fill(floatCentroids, oneCentroidValue);

            yield return floatCentroids;
        }
    }


    async Task<float[]> CalculateCentroid(string url, int centroidNumber, int clusterCount, int iterator)
    {
        var dimensions = await _marketPlaceRepository.GetDimensions(url);
        
        var floatCentroids = new float[dimensions];

        Array.Fill(floatCentroids, 0);

        await foreach(var comment in _marketPlaceRepository.GetAllCommentFromCentroid(centroidNumber, url, clusterCount, iterator))
        {
            
            SumFromCommentEmbeddingsToCentroids(dimensions, comment.Embeddings, floatCentroids);

        }

        floatCentroids = floatCentroids.Select(el => el / dimensions).ToArray();

        return floatCentroids;
    }

    async IAsyncEnumerable<float[]> CalculateCentroids(string url, int iterator)
    {
        var centroidCounts = await _marketPlaceRepository.GetClustersCount(url);

        for(int i = 1; i <= centroidCounts; i++)
        {
            yield return await CalculateCentroid(url, i, centroidCounts, iterator);
        }
    }


    void SumFromCommentEmbeddingsToCentroids(int dimensions, float[] embeddings, float[] floatCentroids)
    {
        for(int i = 0; i < dimensions; i++)
        {
            var fromCommentValue = embeddings[i];
            floatCentroids[i] += fromCommentValue;
        }
    }
    
}
