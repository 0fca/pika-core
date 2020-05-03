using System;
using System.Collections.Generic;
using System.Linq;
using PikaCore.Areas.Api.v1.Data;

namespace PikaCore.Areas.Core.Models.AdminViewModels
{
    public class MessageViewModel
    {
        public IList<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
        public int PageCount { get; set; } = 1;

        public void OrganizeMessages(ref IList<MessageEntity> messages, int messagesPerPageCount)
        {
            messages = messages.OrderBy(m => m.Id).ToList();
            PageCount = (int)Math.Round(messages.Count / (float) messagesPerPageCount, MidpointRounding.AwayFromZero);
        }
    }
}