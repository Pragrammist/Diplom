using Microsoft.AspNetCore.Mvc;
using WebHost.Views.Shared.Components.LabelComment;

namespace WebHost.Views.Components
{
    public class LabelComment : ViewComponent
    {
        public IViewComponentResult Invoke(int ProductId, string Label, double Percent, int? ClusterNumber = null)
        {
            return View(new LabelCommentModel(ProductId, Label, Percent, ClusterNumber));
        }
    }
}
