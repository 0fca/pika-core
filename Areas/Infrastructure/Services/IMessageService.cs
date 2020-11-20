using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PikaCore.Areas.Infrastructure.Data;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IMessageService
    {
        public Task<IList<MessageEntity>> GetAllMessagesForSystem(string systemName);
        
        public Task<IList<MessageEntity>> GetAllMessages();
        
        public Task<IList<IssueEntity>> GetAllIssues(string systemName);

        public Task<MessageEntity> GetMessageById(int id);

        public Task<MessageEntity> GetLatestMessage();

        public Task<IList<IssueEntity>> GetIssuesByMessageId(int id);

        public Task<IssueEntity> GetLatestIssueByMessageId(int id);

        public void ApplyPaging<T>(ref List<T> messageEntities, int count, int offset = 0);
        
        public void ApplyPagingByDate(ref List<MessageEntity> messageEntities, int count, DateTime start, DateTime end);
        
        public Task RemoveMessages(IList<int> ids);

        public Task UpdateMessage(MessageEntity e);

        public Task CreateMessage(MessageEntity e);
    }
}