using Microsoft.AspNetCore.Mvc;

namespace WebHost.Views.Components
{
    public class MarketplaceComment : ViewComponent
    {
        public IViewComponentResult Invoke(CommentData comment)
        {
            return View(comment);
        }
    }
}
