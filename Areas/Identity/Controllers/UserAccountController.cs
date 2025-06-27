using truyenchu.Areas.Identity.Models.UserViewModels;
using truyenchu.Data;
using truyenchu.ExtendMethods;
using truyenchu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using truyenchu.Utilities;
using truyenchu.Areas.Identity.Models.ManageViewModels;

namespace truyenchu.Areas.Identity.Controllers
{

    [Authorize]
    [Area("Identity")]
    [Route("/UserAccount/[action]")]
    public class UserAccountController : Controller
    {

        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public UserAccountController(ILogger<RoleController> logger, RoleManager<IdentityRole> roleManager, AppDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new MyAccountViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = string.Join(", ", roles),
                PasswordModel = new ChangePasswordViewModel() // Khởi tạo model đổi mật khẩu
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccount(MyAccountViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

            // Cập nhật Email và PhoneNumber
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("MyAccount", model);
            }

            TempData["StatusMessageUpdateAccount"] = "Cập nhật thông tin tài khoản thành công.";
            return RedirectToAction(nameof(MyAccount));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(MyAccountViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.PasswordModel.OldPassword, model.PasswordModel.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("MyAccount", model);
            }

            TempData["StatusMessageUpdatePassword"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(MyAccount));
        }

    }
}
