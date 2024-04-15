using Microsoft.AspNetCore.Mvc;
using WebHost.Views.Shared.Components.CommentTag;

namespace WebHost.Views.Components;

public class CommentTag : ViewComponent
{

    public IViewComponentResult Invoke(int productId, string tag, double score, string? label = null, int? clusterNumber = null)
    {
        return View(new CommentTagModel(productId, tag, score, label, clusterNumber));
    }
}

