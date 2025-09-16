using IT15.Models;
using System.Collections.Generic;

namespace IT15.ViewModels.HumanResource
{
    public class OvertimeApprovalViewModel
    {
        public List<DailyLog> AutoDetectedOvertime { get; set; } = new List<DailyLog>();
        public List<OvertimeRequest> ManualRequests { get; set; } = new List<OvertimeRequest>();
    }
}

