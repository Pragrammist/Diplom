namespace WebHost.Models
{
    public record ClusteringResultModel(
        int ProductId,
        ClusteringResultFilter Filter,
        ClusteringResult ClusteringResult
    );
}
