using System;

namespace AspireApp.ApiService.Dto;

public record ProjectMemberDto(Guid Id, Guid ProjectId, string? UserId, string? Email, string Role);
