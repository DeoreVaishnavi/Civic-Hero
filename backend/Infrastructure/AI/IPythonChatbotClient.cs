using CivicHero.Backend.Core.DTOs.Chat;
using System.Threading.Tasks;

namespace CivicHero.Backend.Infrastructure.AI
{
    public interface IPythonChatbotClient
    {
        Task<PythonChatResponseDto> SendMessageAsync(PythonChatRequestDto request);
    }
}