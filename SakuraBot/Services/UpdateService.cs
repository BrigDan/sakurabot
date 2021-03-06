using System;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Sakura.Uwu.Models;
using Sakura.Uwu.GroupManagement;
using Sakura.Uwu.Dota;
using static Sakura.Uwu.Common.Accessories;

namespace Sakura.Uwu.Services 
{
    // Service to respond to API requests
    public class UpdateService : IUpdateService
    {
        private readonly BotDbContext _dbContext;
        private readonly IBotService _botService;
        private readonly IOpenDotaService _dotaService;
        private readonly ILogger<UpdateService> _logger;

        public UpdateService(BotDbContext dbContext, IBotService botService, IOpenDotaService dotaService, ILogger<UpdateService> logger)
        {
            _dbContext = dbContext;
            _botService = botService;
            _logger = logger;
            _dotaService = dotaService;
        }

        public async Task UpdateAsync(Update update)
        {
            Task mainTask = null;
            Task trackTask = null;
            try{
                if (update.Type == UpdateType.Message)
                {
                    var message = update.Message;
                    if (update.Message.Type == MessageType.ChatMembersAdded)
                    {
                        mainTask = Responses.WelcomeResponse(_botService, message, _dbContext);
                    }
                    else if (update.Message.Type == MessageType.Text)
                    {
                        trackTask = Task.Factory.StartNew(() => Tracking.LogUser(_botService, message, _dbContext));
                        if
                        (
                            (message.Entities != null) &&
                            (message.Entities[0].Offset == 0) &&
                            (message.Entities[0].Type == MessageEntityType.BotCommand)
                        )
                        {
                            var commandMetadata = message.Entities[0];
                            var command = message.Text.Substring(commandMetadata.Offset, commandMetadata.Length);
                            var chat = await _botService.Client.GetChatAsync(message.Chat.Id);
                            if (chat.Type == ChatType.Group || chat.Type == ChatType.Supergroup)
                            {
                                if (Commands.User.ContainsKey(command))
                                {
                                    mainTask = Commands.User[command](_botService, message, _dbContext);
                                }
                                else if (Commands.Admin.ContainsKey(command))
                                {
                                    var admins = await _botService.Client.GetChatAdministratorsAsync(message.Chat.Id);
                                    if (admins.Any(admin => admin.User.Id == message.From.Id))
                                    {
                                        var bot = await _botService.Client.GetMeAsync();
                                        var botChatMember = await _botService.Client.GetChatMemberAsync(message.Chat.Id, bot.Id);
                                        if
                                        (
                                            botChatMember.CanPinMessages != null &&
                                            botChatMember.CanRestrictMembers != null &&
                                            (bool) botChatMember.CanPinMessages &&
                                            (bool) botChatMember.CanRestrictMembers
                                        )
                                        {
                                            mainTask = Commands.Admin[command](_botService, message, _dbContext);
                                        }
                                        else
                                        {
                                            await _botService.Client.SendTextMessageAsync(message.Chat.Id, "I would gladly do that for you if you make me Senpai UwU", replyToMessageId: message.MessageId);
                                        }                   
                                    }
                                    else
                                    {
                                        await _botService.Client.SendTextMessageAsync(message.Chat.Id, "Naughty senpai, you aren't admin", replyToMessageId: message.MessageId);                            
                                    }
                                }
                                else if (command.Equals("/dota"))
                                {
                                    var messageParts = message.Text.Split(' ');
                                    if(messageParts.Length >= 1)
                                    {
                                        var matchIdString = messageParts[1];
                                        if(DotaCommands.Commands.ContainsKey(matchIdString))
                                        {
                                            await DotaCommands.Commands[matchIdString](_botService, message, _dotaService);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    var queryData = update.CallbackQuery.Data.Split(' ');
                    var query = update.CallbackQuery;
                    if(queryData.Length == 3)
                    {
                        System.Console.WriteLine(query.Data);
                        // var admins = await _botService.Client.GetChatAdministratorsAsync();
                        // if (admins.Any(admin => admin.User.Id == query.From.Id))
                        // if(update.CallbackQuery)
                    }
                }
                if (mainTask != null)
                {
                    mainTask.Wait();
                }
                if (trackTask != null)
                {
                    trackTask.Wait();
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine($@"Exception Ocurred: {e.Message}");
            }
        }
    }
}