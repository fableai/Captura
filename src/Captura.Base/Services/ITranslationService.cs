using System.Threading.Tasks;

namespace Captura.Services
{
    public interface ITranslationService
    {
        Task Start(string apiKey);
        Task Stop();
        Task StreamAudioData(byte[] buffer, int bytesRecorded);
        bool IsConnected { get; }
    }
}
