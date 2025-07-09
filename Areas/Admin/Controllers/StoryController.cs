using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ImageProcessor;
using ImageProcessor.Imaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using truyenchu.Areas.Admin.Model;
using truyenchu.Data;
using truyenchu.Models;
using truyenchu.Service;
using truyenchu.Utilities;
using truyenchu.Areas.Identity.Models.UserStory;

namespace truyenchu.Areas.Admin.Controllers
{
    [Authorize(Roles = RoleName.Administrator + "," + RoleName.Editor)]
    [Area("Admin")]
    [Route("manage-story/[action]")]
    public class StoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StoryController> _logger;
        private readonly StoryService _storyService;
        public StoryController(AppDbContext context, ILogger<StoryController> logger, StoryService storyService)
        {
            _context = context;
            _logger = logger;
            _storyService = storyService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        // GET: Story
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStories(string searchStr, int pageNumber = 1)
        {
            if (_context.Stories == null)
                return NotFound();

            var qr = _context.Stories
                                    .Include(x => x.Author)
                                    .Include(x => x.Photo)
                                    .Select(s => new
                                    {
                                        s.StoryId,
                                        s.Photo.FileName,
                                        s.Author.AuthorName,
                                        s.StoryName,
                                        s.StorySlug,
                                        s.StoryState,
                                        s.ViewCount,
                                        s.LatestChapterOrder,
                                        s.DateUpdated,
                                        s.Published
                                    })
                                    .OrderByDescending(x => x.DateUpdated)
                                    .AsQueryable();

            dynamic vm;
            if (!string.IsNullOrEmpty(searchStr))
            {
                searchStr = AppUtilities.GenerateSlug(searchStr);
                // find by slug
                vm = await qr.Where(a => a.StorySlug.Contains(searchStr)).ToListAsync();

            }
            else
                vm = await qr.ToListAsync();

            var result = Pagination.PagedResults(vm, pageNumber, Const.STORIES_PER_PAGE_ADMIN);

            return Json(result);
        }

        // api 
        [HttpGet]
        public async Task<IActionResult> GetAuthor(string query)
        {
            if (_context.Stories == null)
                return NotFound();

            if (!string.IsNullOrEmpty(query))
                query = AppUtilities.GenerateSlug(query);

            var authors = await _context.Authors.Where(x => x.AuthorSlug.Contains(query)).Select(x => new { x.AuthorId, x.AuthorName }).ToListAsync();
            return Json(authors);
        }
        // GET: Story/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Stories == null)
            {
                return NotFound();
            }

            var story = await _context.Stories
                .Include(s => s.Author)
                .Include(s => s.Photo)
                .FirstOrDefaultAsync(m => m.StoryId == id);
            if (story == null)
            {
                return NotFound();
            }

            return View(story);
        }

        // GET: Story/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.MultiSelectItem = await GetMultiSelectList();
            return View();
        }

        // POST: Story/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StoryName,AuthorId,Description,StorySource, CategoryIds")] CreateViewModel story, IFormFile? file)
        {
            ViewBag.MultiSelectItem = await GetMultiSelectList();

            if (story.AuthorId == 0)
            {
                ModelState.Remove(nameof(story.AuthorId));
                ModelState.AddModelError(nameof(story.AuthorId), "Phải chọn tác giả");
                return View(story);
            }
            if (ModelState.IsValid)
            {
                var photo = new StoryPhoto();
                photo.FileName = file != null ? AppUtilities.UploadPhoto(file) : Const.STORY_THUMB_NO_IMAGE;
                story.Photo = photo;
                _context.StoryPhotos.Add(photo);


                story.StorySlug = GenerateStorySlug(story.StoryName);
                story.LatestChapterOrder = 0;
                story.DateCreated = DateTime.Now;
                story.DateUpdated = DateTime.Now;
                story.ViewCount = 0;
                story.StoryState = false;

                foreach (int cateId in story.CategoryIds)
                {
                    _context.Add(new StoryCategory() { CategoryId = cateId, Story = story });
                }

                _context.Add(story);
                await _context.SaveChangesAsync();

                StatusMessage = "Thêm truyện mới thành công";
                return RedirectToAction(nameof(Index));
            }

            return View(story);
        }

        // GET: Story/Edit/5
        [Route("{id?}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null || _context.Stories == null)
            {
                return NotFound();
            }

            var story = await _context.Stories.Include(x => x.StoryCategory).Include(x => x.Author).Include(x => x.Photo).FirstOrDefaultAsync(x => x.StoryId == id);
            if (story == null)
            {
                return NotFound();
            }

            var categoryIds = story.StoryCategory.Select(x => x.CategoryId).ToArray();

            var vm = new CreateViewModel(story, categoryIds);

            ViewBag.MultiSelectItem = await GetMultiSelectList();
            return View(vm);
        }

        // POST: Story/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost(Name = "EditStory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm(Name = "StoryId")] int id, [Bind("StoryId,StoryName,AuthorId,Description,StorySource,StoryState,CategoryIds")] CreateViewModel model, IFormFile? file)
        {
            if (id != model.StoryId)
                return NotFound();


            var story = await _context.Stories
                                            .Include(x => x.Author)
                                            .Include(x => x.Photo)
                                            .Include(x => x.StoryCategory)
                                            .FirstOrDefaultAsync(x => x.StoryId == id);
            if (story == null)
                return NotFound();

            var categoryIds = story.StoryCategory.Select(x => x.CategoryId).ToArray();
            var vm = new CreateViewModel(story, categoryIds);
            ViewBag.MultiSelectItem = await GetMultiSelectList();
            if (model.AuthorId == 0)
            {
                ModelState.Remove(nameof(model.AuthorId));
                ModelState.AddModelError(nameof(model.AuthorId), "Phải chọn tác giả");
                return View(vm);
            }

            if (ModelState.IsValid)
            {
                try
                {

                    story.StoryName = model.StoryName;
                    story.StorySource = model.StorySource;
                    story.AuthorId = model.AuthorId;
                    story.Description = model.Description;
                    story.StoryState = model.StoryState;
                    story.DateUpdated = DateTime.Now;

                    // CẬP NHẬT SLUG
                    story.StorySlug = GenerateStorySlug(story.StoryName, story.StoryId);

                    if (file != null)
                    {
                        var photo = await _context.StoryPhotos.FindAsync(story.PhotoId);
                        // Xóa img cũ
                        if (photo != null && photo.FileName != Const.STORY_THUMB_NO_IMAGE)
                        {
                            _context.Remove(photo);
                            AppUtilities.DeletePhoto(photo.FileName);
                        }

                        // Upload mới
                        var newPhoto = new StoryPhoto()
                        {
                            FileName = AppUtilities.UploadPhoto(file)
                        };
                        story.Photo = newPhoto;
                        _context.StoryPhotos.Add(newPhoto);
                    }

                    var oldCateIds = story.StoryCategory.Select(x => x.CategoryId);
                    var newCateIds = model.CategoryIds;

                    var removeCates = story.StoryCategory.Where(x => !newCateIds.Contains(x.CategoryId)).ToList();
                    var addCates = newCateIds.Where(x => !oldCateIds.Contains(x)).ToList();

                    _context.StoryCategories.RemoveRange(removeCates);
                    addCates.ForEach(cate =>
                    {
                        _context.StoryCategories.Add(new StoryCategory()
                        {
                            Story = story,
                            CategoryId = cate
                        });
                    });


                    await _context.SaveChangesAsync();
                    StatusMessage = "Cập nhật truyện thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StoryExists(model.StoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        [HttpPut]
        public async Task<ActionResult> UpdatePublished(int storyId, bool isPublished)
        {
            var story = await _context.Stories.FindAsync(storyId);
            if (story == null)
                return BadRequest();

            story.Published = isPublished;
            await _context.SaveChangesAsync();
            return Ok();
        }


        // GET: Story/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Stories == null)
            {
                return NotFound();
            }

            var story = await _context.Stories
                .Include(s => s.Author)
                .Include(s => s.Photo)
                .FirstOrDefaultAsync(m => m.StoryId == id);
            if (story == null)
            {
                return NotFound();
            }

            return View(story);
        }

        // POST: Story/Delete/5
        [HttpPost(Name = "DeleteStory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int storyId)
        {
            if (_context.Stories == null)
            {
                StatusMessage = "Lỗi: Tập dữ liệu truyện không tồn tại.";
                return Problem("Entity set 'AppDbContext.Stories' is null.");
            }

            // Cần Include tất cả các thực thể con có thể gây xung đột ràng buộc khi xóa Story
            // để có thể kiểm tra hoặc xóa chúng
            var story = await _context.Stories
                                      .Include(s => s.Photo)
                                      .Include(s => s.StoryCategory)
                                      .Include(s => s.Chapters)
                                      .FirstOrDefaultAsync(x => x.StoryId == storyId);

            if (story == null)
            {
                StatusMessage = "Lỗi: Truyện không tìm thấy.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // KIỂM TRA RÀNG BUỘC 1: Kiểm tra xem truyện có còn chương nào không
                if (story.Chapters != null && story.Chapters.Any())
                {
                    StatusMessage = $"Lỗi: Không thể xóa truyện '{story.StoryName}' vì vẫn còn {story.Chapters.Count} chương liên quan. Vui lòng xóa hết các chương hoặc gán chúng cho truyện khác trước khi xóa truyện này.";
                    return RedirectToAction(nameof(Index)); // Ngăn chặn xóa và báo lỗi
                }



                // Nếu đã vượt qua các kiểm tra ràng buộc, tiến hành xóa các phần phụ thuộc trong code
                // (Chỉ xóa những cái mà bạn muốn xóa cùng lúc hoặc đã được kiểm tra ở trên)

                // Xóa ảnh bìa của truyện nếu không phải là ảnh mặc định
                if (story.Photo != null && story.Photo.FileName != Const.STORY_THUMB_NO_IMAGE)
                {
                    AppUtilities.DeletePhoto(story.Photo.FileName);
                    _context.StoryPhotos.Remove(story.Photo);
                }

                // Xóa tất cả các thể loại liên kết với truyện này
                // Mặc dù DataTruyenOnline.sql có ON DELETE CASCADE cho StoryCategory,
                // việc xóa tường minh trong code vẫn đảm bảo tính nhất quán nếu có bất kỳ vấn đề nào.
                if (story.StoryCategory != null && story.StoryCategory.Any())
                {
                    _context.StoryCategories.RemoveRange(story.StoryCategory);
                }

                // Cuối cùng, xóa bản thân truyện
                _context.Stories.Remove(story);
                await _context.SaveChangesAsync();
                StatusMessage = $"Đã xóa truyện '{story.StoryName}' thành công.";
            }
            catch (DbUpdateException ex)
            {
                // Bắt lỗi liên quan đến ràng buộc database khác (phòng trường hợp có ràng buộc chưa lường trước)
                _logger.LogError(ex, "Lỗi khi xóa truyện {StoryId}. Chi tiết: {Message}", storyId, ex.Message);
                StatusMessage = $"Lỗi: Không thể xóa truyện '{story.StoryName}' do lỗi cơ sở dữ liệu. Vui lòng kiểm tra các liên kết khác hoặc liên hệ quản trị viên.";
            }
            catch (Exception ex)
            {
                // Bắt các lỗi không xác định khác
                _logger.LogError(ex, "Lỗi không xác định khi xóa truyện {StoryId}", storyId);
                StatusMessage = $"Lỗi: Đã xảy ra lỗi không mong muốn khi xóa truyện '{story.StoryName}'.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StoryExists(int id)
        {
            return (_context.Stories?.Any(e => e.StoryId == id)).GetValueOrDefault();
        }

        private async Task<MultiSelectList> GetMultiSelectList()
        {
            var categories = await _context.Categories.ToListAsync();
            return new MultiSelectList(categories, nameof(Category.CategoryId), nameof(Category.CategoryName));
        }

        private string GenerateStorySlug(string name, int id = 0)
        {
            var counter = 1;
            var prefix = "";
            var slug = AppUtilities.GenerateSlug(name);
            while (_context.Stories.Any(x => x.StorySlug == slug + prefix && x.StoryId != id))
            {
                prefix = "-" + counter.ToString();
                counter++;
            }
            return slug + prefix;
        }
    }
}
