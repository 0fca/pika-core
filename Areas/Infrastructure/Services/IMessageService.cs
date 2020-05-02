using System.Collections.Generic;
using System.Threading.Tasks;
using PikaCore.Areas.Api.v1.Data;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IMessageService
    {
        public Task<IList<MessageEntity>> GetAllMessages();

        public Task<IList<IssueEntity>> GetAllIssues();

        public Task<MessageEntity> GetMessageById(int id);

        public Task<MessageEntity> GetLatestMessage();

        public Task<IList<IssueEntity>> GetIssuesByMessageId(int id);

        public Task<IssueEntity> GetLatestIssueByMessageId(int id);

        public void ApplyPaging(ref IList<MessageEntity> messageEntities, int count, int offset = 0);
    }
}