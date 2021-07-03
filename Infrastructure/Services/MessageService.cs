using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PikaCore.Areas.Core.Models;
using PikaCore.Infrastructure.Data;

namespace PikaCore.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly SystemContext _systemContext;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public MessageService(SignInManager<ApplicationUser> signInManager,
            SystemContext systemContext)
        {
            _signInManager = signInManager;
            _systemContext = systemContext;
        }

        public async Task<IList<MessageEntity>> GetAllMessagesForSystem(string systemName)
        {
            var messages = 
                PrepareJoin()
                .Where(m => m.SystemDescriptor.SystemName.Equals(systemName))
                .AsQueryable();
            if (_signInManager.Context.User.IsInRole("Admin"))
            { 
                return await messages.ToListAsync();
            }
            return await messages.Where(m => m.IsVisible).ToListAsync();
        }
        
        public async Task<IList<MessageEntity>> GetAllMessages()
        {
            var messages = PrepareJoin();
            if (_signInManager.Context.User.IsInRole("Admin"))
            { 
                return await messages.ToListAsync();
            }
            return await messages.Where(m => m.IsVisible).ToListAsync();
        }

        public async Task<IList<IssueEntity>> GetAllIssues(string name)
        {
            var visibleMessagesList = await _systemContext.Messages.
                Include(m => m.RelatedIssues)
                
                .Where(m => m.IsVisible && m.SystemDescriptor.SystemName.Equals(name)).ToListAsync();
            var issues = new List<IssueEntity>();
            visibleMessagesList.ForEach(m => issues.AddRange(m.RelatedIssues));
            return issues;
        }

        public async Task<MessageEntity> GetMessageById(int id)
        {
            var messages = PrepareJoin();
            if (!_signInManager.Context.User.IsInRole("Admin"))
            {
                messages = messages.Where(m => m.IsVisible);
            }
            return await messages.SingleAsync(m => m.Id == id);
        }

        public async Task<MessageEntity?> GetLatestMessage()
        {
            if(_systemContext.Messages.Any()){
                return await _systemContext.Messages.OrderByDescending(m => m.CreatedAt)
                                .FirstAsync(m => m.IsVisible); 
            }
            return null;
        }
        
        public async Task<IssueEntity> GetLatestIssueByMessageId(int id)
        {
            var list = await _systemContext.Messages
                .Include(m => m.RelatedIssues)
                .ToListAsync();
            var first = list.Find(m => m.Id == id);
            return first != null ? first.RelatedIssues.OrderByDescending(i => i.CreatedAt)
                    .First() : new IssueEntity();
        }

        public async Task<IList<IssueEntity>> GetIssuesByMessageId(int id)
        {
            return (await _systemContext.Messages
                .Include(m => m.RelatedIssues)
                .FirstAsync(m => m.Id == id && m.IsVisible)).RelatedIssues;
        }

        public void ApplyPaging<T>(ref List<T> messageEntities, int count, int offset = 0)
        {
            messageEntities = messageEntities.Count - offset >= count 
                ? messageEntities.ToList().GetRange(offset, count) 
                : messageEntities.ToList().GetRange(offset, messageEntities.Count - offset);
        }

        public void ApplyPagingByDate(ref List<MessageEntity> messageEntities, int count, DateTime start, DateTime end)
        {
            messageEntities = messageEntities.FindAll(me => 
                                                            me.IsVisible 
                                                            && (me.UpdatedAt.Date >= start && me.UpdatedAt.Date <= end)
                                                            );
            ApplyPaging(ref messageEntities, count);
        }

        public async Task RemoveMessages(IList<int> ids)
        {
            _systemContext.Messages
                .RemoveRange(_systemContext.Messages.Where(m => ids.Contains(m.Id)));
            await _systemContext.SaveChangesAsync();
        }

        public async Task UpdateMessage(MessageEntity e)
        {
            var currentMessage = await _systemContext.Messages.FindAsync(e.Id);
            if (currentMessage != null)
            {
                currentMessage.Message = e.Message;
                currentMessage.IsVisible = e.IsVisible;
                currentMessage.RelatedIssues = e.RelatedIssues;
                currentMessage.UpdatedAt = DateTime.Now;
                currentMessage.MessageType = e.MessageType;
                currentMessage.SystemDescriptor = e.SystemDescriptor;
                
                _systemContext.Update(currentMessage);
                await _systemContext.SaveChangesAsync();
            }
        }

        public async Task CreateMessage(MessageEntity e)
        {

            _systemContext.Messages.Update(e);
            await _systemContext.SaveChangesAsync();
        }
        
        #region HelperMethods

        private IQueryable<MessageEntity> PrepareJoin()
        {
            return _systemContext.Messages.Join(_systemContext.Systems,
                m => m.SystemDescriptor.SystemId,
                s => s.SystemId,
                (m, s) => new MessageEntity()
                {
                    Message = m.Message,
                    SystemDescriptor = s,
                    MessageType = m.MessageType,
                    IsVisible = m.IsVisible,
                    Id = m.Id,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    RelatedIssues = m.RelatedIssues
                }
            );
        }

        #endregion
    }
}