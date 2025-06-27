using truyenchu.Models;

namespace truyenchu.Areas.Identity.Models.UserStory
{
    public class UserStory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public int StoryId { get; set; }
        public Story Story { get; set; }
        public DateTime DateAdded { get; set; }
    }
}