using Microsoft.AspNetCore.Mvc;
using WebHost.Views.Shared.Components.ClusterInfo;

namespace WebHost.Views.Components
{
    public class ClusterInfo : ViewComponent
    {
        public IViewComponentResult Invoke(
            int ProductId, 
            int CommentCount,
            TagData[] Tags,
            LabelInfo[] LabelsInfo, 
            string? Label = null, 
            int? ClusterNumber = null
        )
        {
            return View(new ClusterInfoModel(
                ProductId, 
                CommentCount, 
                Tags, LabelsInfo,
                Label, 
                ClusterNumber
            ));
        }
    }
}
