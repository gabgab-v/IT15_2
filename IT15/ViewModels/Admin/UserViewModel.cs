namespace IT15.ViewModels.Admin
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
