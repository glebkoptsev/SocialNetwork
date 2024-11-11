using DialogService.API.DTOs;
using DialogService.API.Services;
using DialogService.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DialogService.API.Controllers
{
    [ApiController]
    [Route("api/dialog"), Authorize]
    public class ChatController(IChatService chatService) : ControllerBase
    {
        private readonly IChatService chatService = chatService;

        [HttpGet, Route("{chat_id}/messages")]
        public async Task<ActionResult<MessageEntity[]>> GetChatAsync(Guid chat_id, int limit = 1000, int offset = 0)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            return Ok(await chatService.GetChatAsync(chat_id, limit, offset, currentUserId));
        }

        [HttpGet, Route("list")]
        public async Task<ActionResult<Chat[]>> GetUserChatListAsync(int offset = 0, int limit = 200)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            return Ok(await chatService.GetUserChatListAsync(currentUserId, limit, offset));
        }

        [HttpPost, Route("{chat_id}/send")]
        public async Task<ActionResult<Guid>> SendMessageToChatAsync([FromRoute] Guid chat_id, [FromBody] SendMessageRequest request)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            return Ok(await chatService.SendMessageToChatAsync(chat_id, request, currentUserId));
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateChatAsync(CreateChatRequest request)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            return Ok(await chatService.CreateChatAsync(request, currentUserId));
        }
    }
}
