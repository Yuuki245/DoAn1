using truyenchu.Areas.Identity.Models;
using truyenchu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using truyenchu.Data;
using truyenchu.Areas.Identity.Models.UserStory;

namespace truyenchu.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/BookStory/[action]")]
    public class BookStoryController : Controller
    {
        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookStoryController(ILogger<RoleController> logger, RoleManager<IdentityRole> roleManager, AppDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var bookStories = await _context.UserStories
                .Include(bs => bs.Story)
                .Where(bs => bs.UserId == user.Id)
                .ToListAsync();

            return View(bookStories);
        }

        [HttpPost]
        public async Task<IActionResult> AddToBookStory(int storyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existingEntry = await _context.UserStories
                .FirstOrDefaultAsync(us => us.UserId == user.Id && us.StoryId == storyId);

            if (existingEntry != null)
            {
                return Json(new { success = false, message = "Truyện này đã có trong tủ!" });
            }

            _context.UserStories.Add(new UserStory
            {
                UserId = user.Id,
                StoryId = storyId,
                DateAdded = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm truyện vào tủ thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromBookStory(int storyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var storyToRemove = await _context.UserStories
                .FirstOrDefaultAsync(us => us.UserId == user.Id && us.StoryId == storyId);

            if (storyToRemove == null)
            {
                return Json(new { success = false, message = "Không tìm thấy truyện trong tủ của bạn!" });
            }

            _context.UserStories.Remove(storyToRemove);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa truyện khỏi tủ thành công!" });
        }

    }
}