﻿namespace DialogService.API.DTOs
{
    public class CreateChatRequest
    {
        public string Name { get; set; } = null!;
        public List<Guid> Users_ids { get; set; } = [];
    }
}
