using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly CivicHeroDbContext _context;
        private readonly IHubProvider _hubProvider; // We'll create a wrapper to get hub context
        private readonly IAwsNotificationProvider _awsProvider;

        public NotificationService(CivicHeroDbContext context, IHubProvider hubProvider, IAwsNotificationProvider awsProvider)
        {
            _context = context;
            _hubProvider = hubProvider;
            _awsProvider = awsProvider;
        }

        public async Task SendNotificationAsync(SendNotificationDto dto)
        {
            // Persist notification
            var notification = new Notification
            {
                UserId = dto.UserId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time via SignalR
            var hubContext = _hubProvider.GetHubContext<CivicHeroHub>();
            await hubContext.Clients.User(dto.UserId.ToString())
                .SendAsync("ReceiveNotification", new NotificationDto
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
                    ReadAt = notification.ReadAt
                });

            // Send via external channels (email/sms) if requested
            foreach (var channel in dto.Channels)
            {
                switch (channel)
                {
                    case NotificationChannel.Email:
                        // We need user email; fetch from user
                        var user = await _context.Users.FindAsync(dto.UserId);
                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        {
                            await _awsProvider.SendEmailAsync(user.Email, dto.Title, dto.Message);
                        }
                        break;
                    case NotificationChannel.SMS:
                        var userForSms = await _context.Users.FindAsync(dto.UserId);
                        if (userForSms != null && !string.IsNullOrWhiteSpace(userForSms.PhoneNumber))
                        {
                            await _awsProvider.SendSmsAsync(userForSms.PhoneNumber, $"{dto.Title}: {dto.Message}");
                        }
                        break;
                    case NotificationChannel.InApp:
                        // Already handled via SignalR and DB persistence
                        break;
                }
            }
        }

        public async Task<List<NotificationDto>> GetNotificationsForUserAsync(int userId, bool includeRead = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            return notifications;
        }

        public async Task MarkAsReadAsync(int userId, MarkReadDto dto)
        {
            if (dto.NotificationIds == null || !dto.NotificationIds.Any())
                return;

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && dto.NotificationIds.Contains(n.Id))
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}