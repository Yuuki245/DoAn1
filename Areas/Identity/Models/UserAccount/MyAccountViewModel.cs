using System.ComponentModel.DataAnnotations;
using truyenchu.Areas.Identity.Models.ManageViewModels;

namespace truyenchu.Areas.Identity.Models.ManageViewModels
{
    public class MyAccountViewModel
    {
        // Thông tin tài khoản
        public string UserName { get; set; }

        [Required(ErrorMessage = "Phải nhập {0}")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phải nhập {0}")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public string Roles { get; set; }

        // Model cho đổi mật khẩu
        public ChangePasswordViewModel PasswordModel { get; set; }
    }
}

