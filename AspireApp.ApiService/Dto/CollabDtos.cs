using System;

namespace AspireApp.ApiService.Dto;

public record ProjectCollabDto(Guid Id, string Name, string? Description);
public record TaskCollabDto(Guid Id, string Title, string? Description, int Status, Guid ProjectId, string? AssignedToId);
