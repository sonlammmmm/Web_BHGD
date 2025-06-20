namespace Web_BHGD.Areas.Admin.Models
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public List<RoleViewModel> Roles { get; set; } = new List<RoleViewModel>(); // Khởi tạo mặc định
    }

    public class RoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}