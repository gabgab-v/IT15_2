using IT15.Models;

namespace IT15.ViewModels
{
    public class ResignationViewModel
    {
        // This will hold an existing request to display its status
        public ResignationRequest ExistingRequest { get; set; }

        // This will be used to bind to the new resignation form
        public ResignationRequest NewRequest { get; set; }
    }
}
