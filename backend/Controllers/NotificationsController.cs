using CitizenHero.Backend.Core.DTOs.Notifications;
using CitizenHero.Backend.Core.Entities;
using CitizenHero.Backend.Core.Enums;
using CitizenHero.Backend.Core.Interfaces;
using CitizenHero.Backend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;

        public NotificationsController(INotificationService notificationService, IUserRepository userRepository)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications([FromQuery] bool includeRead = false)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var notifications = await _notificationService.GetNotificationsForUserAsync(userId, includeRead);
            return Ok(notifications);
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        // PATCH: api/notifications/read
        [HttpPatch("read")]
        public async Task<ActionResult> MarkAsRead([FromBody] MarkReadDto dto)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(userId, dto);
            return NoContent();
        }

        // POST: api/notifications/send
        [HttpPost("send")]
        public async Task<ActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = userId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                Channels = dto.Channels
            });

            return Ok();
        }

        // POST: api/notifications/broadcast (admin only)
        [HttpPost("broadcast")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> BroadcastNotification([FromBody] SendNotificationDto dto)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized();

            if (user.Role != Role.Admin)
                return Forbid();

            var users = await _userRepository.GetAllAsync();
            foreach (var recipient in users)
            {
                await _notificationService.SendNotificationAsync(new SendNotificationDto
                {
                    UserId = recipient.Id,
                    Type = dto.Type,
                    Title = dto.Title,
                    Message = dto.Message,
                    Channels = dto.Channels
                });
            }

            return Ok(new { Message = $"Broadcast sent to {users.Count} users" });
        }
    }
}