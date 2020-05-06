using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Infrastructure.Data;

namespace PikaCore.Areas.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageContext _messageContext;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public MessageService(MessageContext messageContext,
            SignInManager<ApplicationUser> signInManager)
        {
            _messageContext = messageContext;
            _signInManager = signInManager;
        }

        public async Task<IList<MessageEntity>> GetAllMessages()
        {
            var messages = _messageContext.Messages
                .Include( m => m.SystemDescriptor)
                .AsQueryable();
            if (_signInManager.Context.User.IsInRole("Admin"))
            { 
                return await messages.ToListAsync();
            }
            return await messages.Where(m => m.IsVisible).ToListAsync();
        }

        public async Task<IList<IssueEntity>> GetAllIssues()
        {
            var visibleMessagesList = await _messageContext.Messages.
                Include(m => m.RelatedIssues)
                .Where(m => m.IsVisible).ToListAsync();
            var issues = new List<IssueEntity>();
            visibleMessagesList.ForEach(m => issues.AddRange(issues));
            return issues;
        }

        public async Task<MessageEntity> GetMessageById(int id)
        {
            var messages = _messageContext.Messages
                .Include( m => m.SystemDescriptor)
                .AsQueryable();
            if (!_signInManager.Context.User.IsInRole("Admin"))
            {
                messages = messages.Where(m => m.IsVisible);
            }
            return await messages.SingleAsync(m => m.Id == id);
        }

        public async Task<MessageEntity> GetLatestMessage()
        {
            return await _messageContext.Messages.OrderByDescending(m => m.CreatedAt)
                .FirstAsync(m => m.IsVisible);
        }
        
        public async Task<IssueEntity> GetLatestIssueByMessageId(int id)
        {
            return (await _messageContext.Messages.FindAsync(id))
                .RelatedIssues.OrderByDescending(i => i.CreatedAt)
                .First();
        }

        public async Task<IList<IssueEntity>> GetIssuesByMessageId(int id)
        {
            return (await _messageContext.Messages
                .Include(m => m.RelatedIssues)
                .FirstAsync(m => m.Id == id && m.IsVisible)).RelatedIssues;
        }

        public void ApplyPaging(ref IList<MessageEntity> messageEntities, int count, int offset = 0)
        {
            messageEntities = messageEntities.Count - offset >= count 
                ? messageEntities.ToList().GetRange(offset, count) 
                : messageEntities.ToList().GetRange(offset, messageEntities.Count);
        }

        public async Task RemoveMessages(IList<int> ids)
        {
            _messageContext.Messages
                .RemoveRange(_messageContext.Messages.Where(m => ids.Contains(m.Id)));
            await _messageContext.SaveChangesAsync();
        }

        public async Task UpdateMessage(MessageEntity e)
        {
            var currentMessage = await _messageContext.Messages.FindAsync(e.Id);
            if (currentMessage != null)
            {
                currentMessage.Message = e.Message;
                currentMessage.IsVisible = e.IsVisible;
                currentMessage.RelatedIssues = e.RelatedIssues;
                currentMessage.UpdatedAt = DateTime.Now;
                currentMessage.MessageType = e.MessageType;
                currentMessage.SystemDescriptor = e.SystemDescriptor;
                
                _messageContext.Update(currentMessage);
                await _messageContext.SaveChangesAsync();
            }
        }

        public async Task CreateMessage(MessageEntity e)
        {
            _messageContext.Messages.Update(e);
            await _messageContext.SaveChangesAsync();
        }
    }
}