namespace CheckAppBackend.Application.Dtos;

public record RegisterRequestDto(string Email, string Password, string FullName);

public record LoginRequestDto(string Email, string Password);

public record UserDto(Guid Id, string Email, string FullName, string Role);

public record AuthResponseDto(string Token, UserDto User);
