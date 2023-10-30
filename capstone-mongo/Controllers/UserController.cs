using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using capstone_mongo.Models;
using capstone_mongo.Services;
using capstone_mongo.Helper;
using MongoDB.Driver;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace capstone_mongo.Controllers
{
    public class UserController : Controller
    {
        private readonly IConfiguration config;

        private readonly UserService userService;
        private readonly SessionService sessionService;

        public UserController(IConfiguration config,
                              UserService userService,
                              SessionService sessionService)
        {
            this.config = config;
            this.userService = userService;
            this.sessionService = sessionService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string UserId, string Password)
        {
            // for empty fields
            if (!ModelState.IsValid)
            {
                return View();
            }

            // authenticating user
            try
            {
                var res = await userService.AuthenticateUser(UserId, Password);

                if (res != null)
                {
                    MongoConfig.SetLoginStatus(true);
                    HttpContext.Session.SetString("moduleCode", res.Module);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, res.UserId),
                        new Claim(ClaimTypes.Role, res.Role),
                        new Claim(ClaimTypes.GivenName, res.Name)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "Login");
                    var authProperties = new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(20),
                        IsPersistent = false
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                }

                if (res.Role == "admin")
                {
                    TempData["redirected"] = true;
                    return RedirectToAction("Index", "Module");
                }
                else
                {
                    TempData["redirected"] = true;
                    return RedirectToAction("Details", "Module",
                        new { id = sessionService.ModuleCode });
                }
            }
            catch (InvalidCredentialException)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {

            // clear session
            HttpContext.Session.Clear();

            // clear authentication cookies 
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            MongoConfig.SetLoginStatus(false);

            // switch back to UsersDB
            MongoConfig.GetUserDatabase();

            return RedirectToAction("Login");
        }
    }
}





