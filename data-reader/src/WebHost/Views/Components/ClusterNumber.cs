using Microsoft.AspNetCore.Mvc;
using WebHost.Views.Shared.Components.ClusterNumber;

namespace WebHost.Views.Components
{
    public class ClusterNumber : ViewComponent
    {        
        public IViewComponentResult Invoke(int ProductId, int ClusterNumber, string? Label = null)
        {
            return View(new ClusterNumberModel(ProductId, ClusterNumber, Label));
        }
    }
}
