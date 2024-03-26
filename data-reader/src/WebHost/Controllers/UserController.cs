using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WebHost.Infrastructure.DataAccess;

namespace WebHost.Controllers
{

    public class UserController : Controller
    {
        readonly AppDbContext _dbContext;
        public UserController (AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        // GET: UserContorller
        [Authorize]
        public async Task<ActionResult> Index()
        {
            var user = await GetCurrentUser();
            return View(user);
        }

        async Task<User> GetCurrentUser()
        {
            var email = User.FindFirst(ClaimsIdentity.DefaultNameClaimType) ?? throw new Exception("User not authorized");
            var user = await _dbContext.Users.Include(u => u.Products).FirstAsync(u => u.Email == email.Value);
            return user;
        }

        // GET: UserContorller/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserContorller/Create
        //[HttpGet("/User/Create")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserContorller/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(User user)
        {
            try
            {
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();
                await Authenticate(user);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(User user)
        {
            try
            {
                var fuser = await _dbContext.Users.FirstAsync(u => u.Email == user.Email && u.Password == user.Password);
                await Authenticate(user);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        
        
        public ActionResult Login()
        {
            return View();
        }


        private async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        // GET: UserContorller/Edit/5
        [Authorize]
        public async Task<ActionResult> Edit()
        {
            try
            {
                var fuser = await GetCurrentUser();
                
                
                return View(fuser);
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserContorller/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(User user)
        {
            try
            {
                var fuser = await GetCurrentUser();
                fuser.Email = user.Email;
                await _dbContext.SaveChangesAsync();
                await Authenticate(user);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserContorller/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserContorller/Delete/5
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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "User");
        }

    }
}
