using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Sakura.Uwu.Services;
using Sakura.Uwu.Models;

namespace Sakura.Uwu.GroupManagement
{
    static class Tracking
    {
        public static void LogUser(IBotService botService, Message message, BotDbContext dbContext)
        {
            try
            {
                var lookupTable = dbContext.Lookup;
                var entry = lookupTable.FirstOrDefault(user => user.UserId == message.From.Id);
                if(message.From.Username != null) {
                    if(entry == null) {
                        lookupTable.Add(new UserLookup(message.From.Id, message.From.Username));
                    } else if(entry.UserName != message.From.Username) {
                        entry.UserName = message.From.Username;
                    }
                }
                dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Tracking : {0}", e.Message);
            }
        }
    }
}