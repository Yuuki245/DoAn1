using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using truyenchu.Area.ViewStory.Model;
using truyenchu.Data;
using truyenchu.Models;
using truyenchu.Service;
using truyenchu.Utilities;
using truyenchu.Areas.ViewStory.Models.ReadingHistory;
using System.Security.Claims;

namespace truyenchu.Area.ViewStory.Controllers
{
    [Area("ViewStory")]
    public class ViewStoryController : Controller
    {
        private readonly ILogger<ViewStoryController> _logger;
        private readonly AppDbContext _context;
        private readonly StoryService _storyService;

        public ViewStoryController(ILogger<ViewStoryController> logger, AppDbContext dbContext, StoryService storyService)
        {
            _logger = logger;
            _context = dbContext;
            _storyService = storyService;
        }

        [Route("/")]
        public async Task<IActionResult> Index()
        {
            var vm = new IndexViewModel();

            // Lấy danh sách lịch sử đọc từ cơ sở dữ liệu
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var readingHistories = await _context.ReadingHistory
                        .Where(x => x.UserId == userId)
                        .OrderByDescending(x => x.LatestReading)
                        .ToListAsync();

                    vm.ReadingStories = readingHistories.Select(x => new ReadingStory
                    {
                        StorySlug = x.StorySlug,
                        ChapterOrder = x.ChapterOrder,
                        LatestReading = x.LatestReading,
                        StoryName = _context.Stories.FirstOrDefault(s => s.StorySlug == x.StorySlug)?.StoryName
                    }).ToList();
                }
            }

            // Lấy danh sách các danh mục
            var categories = await _context.Categories.ToListAsync();
            vm.SelectListItems = new SelectList(categories, nameof(Category.CategorySlug), nameof(Category.CategoryName));
            return View(vm);
        }

        [Route("api/user-reading-history")]
        [HttpGet]
        public async Task<IActionResult> GetUserReadingHistory(string userId)
        {
            var histories = await _context.ReadingHistory
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.LatestReading)
                .Select(x => new
                {
                    x.StorySlug,
                    x.ChapterOrder,
                    x.LatestReading
                })
                .ToListAsync();

            return Json(histories);
        }

        [Route("{storySlug?}")]
        public async Task<IActionResult> DetailStory([FromRoute] string storySlug)
        {
            if (storySlug == null)
                return NotFound();
            var story = _context.Stories
                        .Where(x => x.Published && x.StorySlug == storySlug)
                        .Include(x => x.Author)
                        .Include(x => x.Photo)
                        .Include(x => x.StoryCategory)
                        .ThenInclude(x => x.Category)
                        .AsQueryable()
                        .FirstOrDefault();
            if (story == null)
                return NotFound();

            var vm = new DetailViewModel();
            var chapters = await (from chapter in _context.Chapters
                                  where chapter.StoryId == story.StoryId
                                  orderby chapter.Order
                                  select new Chapter
                                  {
                                      Order = chapter.Order,
                                      Title = chapter.Title
                                  }).ToListAsync();

            vm.Chapters = chapters;
            vm.Story = story;
            ViewBag.Story = story;
            ViewBag.breadcrumbs = new List<BreadCrumbModel>(){
                new BreadCrumbModel() {},
                new BreadCrumbModel() {DisplayName = story.StoryName, IsActive = true}
            };
            return View(vm);
        }

        [Route("{storySlug?}/chuong-{chapterOrder}")]
        public async Task<IActionResult> Chapter(string storySlug, int chapterOrder)
        {
            var vm = new ChapterViewModel();
            var story = await _context.Stories.FirstOrDefaultAsync(x => x.Published && x.StorySlug == storySlug);
            if (story == null)
                return NotFound();

            var chapter = await _context.Chapters.FirstOrDefaultAsync(x => x.StoryId == story.StoryId && x.Order == chapterOrder);
            if (chapter == null)
            {
                return RedirectToAction(nameof(DetailStory), new { storySlug = story.StorySlug });
            }

            // Lưu lịch sử đọc của user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await SaveReadingHistory(userId, story, chapter);
            }

            vm.Story = story;
            vm.Chapter = chapter;
            ViewBag.breadcrumbs = new List<BreadCrumbModel>()
            {
                new BreadCrumbModel(),
                new BreadCrumbModel { DisplayName = story.StoryName , Url = Url.Action(nameof(DetailStory), new { storySlug = story.StorySlug }) },
                new BreadCrumbModel { DisplayName = "Chương " + chapter.Order, IsActive = true }
            };

            return View(vm);
        }

        private async Task SaveReadingHistory(string userId, Story story, Chapter chapter)
        {
            // Kiểm tra xem lịch sử đã tồn tại chưa
            var existingHistory = await _context.ReadingHistory
                .FirstOrDefaultAsync(x => x.UserId == userId && x.StorySlug == story.StorySlug);

            if (existingHistory != null)
            {
                // Cập nhật thông tin nếu đã tồn tại
                existingHistory.ChapterOrder = chapter.Order;
                existingHistory.LatestReading = DateTime.Now;
            }
            else
            {
                // Thêm mới nếu chưa tồn tại
                var newHistory = new ReadingHistory
                {
                    UserId = userId,
                    StorySlug = story.StorySlug,
                    ChapterOrder = chapter.Order,
                    LatestReading = DateTime.Now
                };
                _context.ReadingHistory.Add(newHistory);

                // Giới hạn tối đa 5 lịch sử đọc gần nhất
                var userHistories = await _context.ReadingHistory
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.LatestReading)
                    .ToListAsync();

                if (userHistories.Count > 5)
                {
                    var oldestHistory = userHistories.Last();
                    _context.ReadingHistory.Remove(oldestHistory);
                }
            }
            await _context.SaveChangesAsync();
        }

        [Route("api/get-chapter")]
        [HttpGet]
        public async Task<IActionResult> GetChapterAPI(string storySlug, int pageNumber = 1)
        {
            var story = await _context.Stories.FirstOrDefaultAsync(x => x.StorySlug == storySlug);
            if (story == null)
            {
                _logger.LogInformation("Story not found");
                return BadRequest();
            }

            var chapters = await _context.Chapters.Where(x => x.StoryId == story.StoryId)
                            .Select(x => new Chapter { Order = x.Order, Title = x.Title })
                            .OrderBy(x => x.Order)
                            .ToListAsync();
            var pageSize = Const.CHAPTER_PER_PAGE;
            Pagination.PagingData<Chapter> pagedData = Pagination.PagedResults(chapters, pageNumber, pageSize);
            return Json(pagedData);
        }

        //private string GenerateCookieJson(Story story, Chapter chapter)
        //{
        //    var cookieItem = new ReadingStory()
        //    {
        //        StoryName = story.StoryName,
        //        StorySlug = story.StorySlug,
        //        ChapterOrder = chapter.Order,
        //        LatestReading = DateTime.Now
        //    };

        //    var cookie = Request.Cookies[Const.READING_STORY_COOKIE_NAME];
        //    if (cookie == null)
        //        return JsonConvert.SerializeObject(new List<ReadingStory>() { cookieItem });

        //    var list = JsonConvert.DeserializeObject<List<ReadingStory>>(cookie);
        //    if (list.Any(x => x.StorySlug == story.StorySlug))
        //    {
        //        var updateItem = list.FirstOrDefault(x => x.StorySlug == story.StorySlug);
        //        updateItem.ChapterOrder = chapter.Order;
        //        updateItem.LatestReading = DateTime.Now;
        //    }
        //    else
        //    {
        //        list.Add(cookieItem);
        //        // max length is 5 story reading recently
        //        if (list.Count() > 5)
        //        {
        //            var removeItem = list.OrderBy(x => x.LatestReading).FirstOrDefault();
        //            list.Remove(removeItem);
        //        }

        //    }
        //    return JsonConvert.SerializeObject(list);
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}


