namespace WebHost.Views.Shared.Components.ClusterInfo;

public record ClusterInfoModel(int ProductId, int CommentCount, IEnumerable<TagData> Tags, IEnumerable<LabelInfo> Labels, string? FilterLabel = null, int? ClusterNumber = null);
