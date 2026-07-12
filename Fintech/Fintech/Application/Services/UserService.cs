using Fintech.Core.Domain;
using Fintech.Infrastructure.Persistence;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fintech.Application.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByCodeAsync(string code);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User> CreateAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinVedaDbContext _dbContext;
    private readonly ICodeGenerationService _codeService;
    private readonly ITenantService _tenantService;

    public UserService(
        IUnitOfWork unitOfWork,
        FinVedaDbContext dbContext,
        ICodeGenerationService codeService,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _codeService = codeService;
        _tenantService = tenantService;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _unitOfWork.Repository<User>().GetByIdIgnoreFiltersAsync(id);
    }

    public async Task<User?> GetByCodeAsync(string code)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserCode == code && u.BranchId == _tenantService.BranchId);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _unitOfWork.Repository<User>().ListAllAsync();
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        user.Id = Guid.NewGuid();
        user.UserCode = await _codeService.GenerateCodeAsync("User", _tenantService.BranchId);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.IsActive = true;
        
        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.CompleteAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        await _unitOfWork.Repository<User>().UpdateAsync(user);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        // Use DbContext directly to ensure EF tracking picks up the change
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        
        user.IsActive = false;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Soft delete via EF context (not UoW, to ensure tracking)
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user != null)
        {
            user.IsActive = false;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}

