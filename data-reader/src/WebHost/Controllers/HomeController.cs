﻿using Microsoft.AspNetCore.Mvc;

namespace WebHost.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
