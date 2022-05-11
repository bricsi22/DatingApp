using Microsoft.AspNetCore.SignalR;
using API.Interfaces;
using API.DTOs;
using AutoMapper;
using System.Threading.Tasks;
using System;
using API.Extensions;
using API.Entities;
using System.Linq;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMapper _mapper;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;
        public MessageHub(IMessageRepository messageRepository,
            IUserRepository userRepository,
            IHubContext<PresenceHub> presenceHub,
            IMapper mapper,
            PresenceTracker tracker)
        {
            this._presenceHub = presenceHub;
            this._userRepository = userRepository;
            this._messageRepository = messageRepository;
            this._mapper = mapper;
            this._tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDto.RecipientUsername.ToLower())
            {
                throw new HubException("You cannot send message to yourself");
            }

            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recepient = await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recepient == null)
            {
                throw new HubException("Not found user");
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recepient,
                SenderUsername = sender.UserName,
                RecipientUsername = recepient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recepient.UserName);
            var group = await _messageRepository.GetMessageGroup(groupName);
            if (group.Connections.Any(x => x.Username == recepient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await _tracker.GetConnectionsForUser(recepient.UserName);
                if (connections != null)
                {
                    await _presenceHub
                        .Clients
                        .Clients(connections)
                        .SendAsync("NewMessageReceived",
                            new {
                                username = sender.UserName,
                                knownAs = sender.KnownAs
                            });
                }
            }
            _messageRepository.AddMessage(message);

            if (await _messageRepository.SaveAllAsync())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
            else 
            {
                throw new HubException("Failed to send message");
            }
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if (group == null)
            {
                group = new Group(groupName);
                _messageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if (await _messageRepository.SaveAllAsync())
            {
                return group;
            }

            throw new HubException("Failed to join group");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _messageRepository.RemoveConnection(connection);
            if (await _messageRepository.SaveAllAsync()) {
                return group;
            }
            throw new HubException("Failed to remove from group");
        }

        private string GetGroupName(string callerUsername, string otherUsername)
        {
            var stringCompare = string.CompareOrdinal(callerUsername, otherUsername) < 0;
            return stringCompare ? $"{callerUsername}-{otherUsername}" : $"{otherUsername}-{callerUsername}";
        }
    }
}