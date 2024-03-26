using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHost.Models;

namespace WebHost.Controllers
{
    public class ProductController : Controller
    {
        // GET: ProductController

        readonly AppDbContext _appDbContext;

        readonly IMarketPlaceRepository _marketPlaceRepository;

        public ProductController(AppDbContext appDbContext, IMarketPlaceRepository marketPlaceRepository)
        {
            _appDbContext = appDbContext;
            _marketPlaceRepository = marketPlaceRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _appDbContext.Products.ToArrayAsync();
            return View(products);
        }


        // GET: ProductController/Details/5
        public async Task<IActionResult> Details(int productId, string? label = null, int clusterNumber = 0, int page = 1,  string? tag = null, string? searchStr = null)
        {
            try
            {
                var product = await _appDbContext.Products.FirstAsync(p => p.ProductId == productId);
                var filter = new ClusteringResultFilter(
                    product.Url,
                    product.ProductName,
                    tag,
                    searchStr,
                    label,
                    clusterNumber,
                    page
                );
                if(product.IsLoaded)
                {
                    var analisResult = await _marketPlaceRepository.CommentsAnalisResult(filter);
                    var model = new ClusteringResultModel(productId, filter, analisResult);
                    return View(model);
                }

                return View();
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
            
        }

        // GET: ProductController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProductController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(Product product)
        {   
            try
            {
                var user = await GetCurrentUser();
                product.User = user;
                product.IsLoaded = false;
                await _appDbContext.Products.AddAsync(product);
                await _appDbContext.SaveChangesAsync();
                BackgroundJob.Enqueue<MarketPlacesObservable>(s => s.NotifyObservers(new MarketplaceIndetificator(product.Url, "yandex-maket", string.Empty)));
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        async Task<User> GetCurrentUser()
        {
            var email = User.FindFirst(ClaimsIdentity.DefaultNameClaimType) ?? throw new Exception("User not authorized");
            var user = await _appDbContext.Users.FirstAsync(u => u.Email == email.Value);
            return user;
        }

        // GET: ProductController/Edit/5
        [Authorize]
        public async Task<ActionResult> Edit(int productId)
        {
            var user = await GetCurrentUser();

            if (user.Email != "moderator@gmail.com")
                return RedirectToAction(nameof(Index));


            var product = await _appDbContext.Products.FirstAsync(p => p.ProductId == productId);




            return View(product);
        }

        // POST: ProductController/Edit/5
        [Authorize]
        [HttpPost("/Product/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int productId, Product productData)
        {
            try
            {
                var product = await _appDbContext.Products.FirstAsync(p => p.ProductId == productData.ProductId);
                product.ProductName = productData.ProductName;
                await _appDbContext.SaveChangesAsync();
                
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProductController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ProductController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
