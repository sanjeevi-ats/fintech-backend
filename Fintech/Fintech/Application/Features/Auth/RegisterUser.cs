using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Fintech.Core.Domain;
using MediatR;
using Microsoft.Extensions.Configuration;
using Npgsql;

using BCrypt.Net;

namespace Fintech.Application.Features.Auth
{
    public class RegisterUserCommand : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
        public UserRole Role { get; set; }
    }

    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Guid>
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public RegisterUserHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=Fintech;Username=postgres;Password=Test123";
        }

        public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            const string sql = @"
                INSERT INTO users (id, branch_id, name, email, password_hash, role, is_active, totp_enabled, refresh_token)
                VALUES (@Id, @BranchId, @Name, @Email, @Password, @Role, @IsActive, @TotpEnabled, @RefreshToken)
                RETURNING id;";

            var userId = await connection.QuerySingleAsync<Guid>(sql, new {
                Id = Guid.NewGuid(),
                BranchId = request.BranchId,
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password), 
                Role = request.Role.ToString(),
                IsActive = true,
                TotpEnabled = false,
                RefreshToken = string.Empty
            });

            return userId;
        }
    }
}
