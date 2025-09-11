using System.Threading.Tasks;

namespace IT15.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}

