using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using truyenchu.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using truyenchu.Area.ViewStory.Model;

namespace truyenchu.Areas.ViewStory.Models.ReadingHistory
{
    public class ReadingHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string StorySlug { get; set; }
        public int ChapterOrder { get; set; }
        public DateTime LatestReading { get; set; }
    }
}
