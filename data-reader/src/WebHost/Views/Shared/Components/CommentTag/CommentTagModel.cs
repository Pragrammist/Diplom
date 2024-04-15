namespace WebHost.Views.Shared.Components.CommentTag
{
    public record CommentTagModel (
        int ProductId, 
        string Tag, 
        double Score, 
        string? Label = null, 
        int? ClusterNumber = null
    );
    
}
