using Microsoft.AspNetCore.Mvc;
using WebHost.Views.Shared.Components.CounterComponent;

namespace WebHost.Views.Components
{
    public class Counter : ViewComponent
    {
        public IViewComponentResult Invoke(int ProductId, int CommentCount, int? ClusterNumber = null)
        {
            return View(new CounterModel(ProductId, CommentCount, ClusterNumber));
        }
    }
}
