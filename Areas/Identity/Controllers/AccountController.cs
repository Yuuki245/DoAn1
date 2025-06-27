// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using truyenchu.Areas.Identity.Models.AccountViewModels;
using truyenchu.Data;
using truyenchu.ExtendMethods;
using truyenchu.Models;
using truyenchu.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using truyenchu.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace truyenchu.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/Account/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<AccountController> logger,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet("/login/")]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        //[HttpPost("/login/")]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        //{
        //    returnUrl ??= Url.Action("Index", "Admin", new { area = "Admin" });
        //    _logger.LogInformation(returnUrl);
        //    ViewData["ReturnUrl"] = returnUrl;
        //    if (ModelState.IsValid)
        //    {
        //        var result = await _signInManager.PasswordSignInAsync(model.UserNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: true);

        //        if (result.Succeeded)
        //        {
        //            _logger.LogInformation(1, "User logged in.");
        //            return LocalRedirect(returnUrl);
        //        }

        //        if (result.IsLockedOut)
        //        {
        //            _logger.LogWarning(2, "Tài khoản bị khóa");
        //            return View("Lockout");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("Không đăng nhập được.");
        //            return View(model);
        //        }
        //    }
        //    return View(model);
        //}
        [HttpPost("/login/")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (returnUrl == null)
            {
                returnUrl = Url.Action("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Tìm thông tin người dùng
                    var user = await _userManager.FindByNameAsync(model.UserNameOrEmail);

                    // Thêm Claims
                    var roles = await _userManager.GetRolesAsync(user); // Lấy vai trò của người dùng
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id), // Thêm UserId vào NameIdentifier
                        new Claim(ClaimTypes.Name, user.UserName)      // Giữ nguyên UserName
                    };
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role)); // Thêm từng role
                    }

                    // Tạo ClaimsIdentity
                    var claimsIdentity = new ClaimsIdentity(claims, "login");

                    // Đăng nhập kèm Claims
                    await HttpContext.SignInAsync(
                        IdentityConstants.ApplicationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTime.UtcNow.AddHours(1)
                        });

                    // Ghi log kiểm tra vai trò
                    if (await _userManager.IsInRoleAsync(user, "Administrator"))
                    {
                        _logger.LogInformation("User {UserName} is an Administrator.", user.UserName);
                    }
                    else
                    {
                        _logger.LogInformation("User {UserName} logged in without Administrator role.", user.UserName);
                    }

                    return LocalRedirect(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Tài khoản bị khóa.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                    return View(model);
                }
            }

            return View(model);
        }

        // GET: /Account/Register
        [HttpGet("/register/")]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost("/register/")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            if (returnUrl == null)
            {
                returnUrl = Url.Action("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var newUser = new AppUser
                {
                    UserName = model.UserName,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(newUser, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account.");
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    if (error.Description.Contains("is already taken"))
                    {
                        ModelState.AddModelError("UserName", "Tên người dùng này đã được sử dụng. Vui lòng chọn tên khác.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(model);
        }

        // POST: /Account/LogOff
        //[HttpPost("/logout/")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> LogOff(string returnUrl = null)
        //{
        //    returnUrl ??= Url.Action(nameof(Login));
        //    await _signInManager.SignOutAsync();
        //    _logger.LogInformation("User đăng xuất");
        //    return LocalRedirect(returnUrl);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/Account/LogOff")]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync(); // Xóa session người dùng
            Response.Cookies.Delete(".AspNetCore.Identity.Application"); // Xóa cookie
            _logger.LogInformation("User logged out.");
            return Redirect("/"); // Quay lại trang chủ
        }

        [Route("/khongduoctruycap.html")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
