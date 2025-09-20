namespace JobScheduling.API.Models;

public record SomethingDto(Guid Id, DateTime CreatedAt, string Message = "");
