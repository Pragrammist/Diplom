using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace WebHost.Views.Components
{
    public class ProductsInfo : ViewComponent
    {
        readonly AppDbContext _db;
        public ProductsInfo(AppDbContext db)
        {
            _db = db;
        }
        public async Task<IViewComponentResult> InvokeAsync(
            int Page = 1,
            bool ForCurrentUser = false
        )
        {
            Page = Page < 0 ? 1 : Page;

            IQueryable<Product> productQuery = _db.Products;

            var currentUser = await GetCurrentUser();

            if (ForCurrentUser)
                productQuery = productQuery.Include(p => p.User).Where(p => p.User.UserId == currentUser.UserId);

            const int pageSize = 10;

            var products = productQuery.Skip(pageSize * (Page - 1)).Take(pageSize);

            

            return View(products);
        }

        async Task<User> GetCurrentUser()
        {
            var email = User.Identity?.Name ?? throw new Exception("User not authorized");

            var user = await _db.Users.Include(u => u.Products).FirstAsync(u => u.Email == email);

            return user;
        }
    }
}
