using System;
using System.Text;
using System.Reflection;
using StackExchange.Redis;
using FluentValidation;
using MediatR;
using Fintech;
using Fintech.Application.Services;
using Fintech.Infrastructure.Persistence;
using Fintech.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Fintech.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinVeda API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure NpgsqlDataSource for Enums
// If env var is missing or still points to Neon (which has channel_binding issues), use Render PostgreSQL
const string RENDER_PG = "Host=dpg-d9chkoe7r5hc73bdlmtg-a.oregon-postgres.render.com;Database=fintech_db_4tu1;Username=fintech_db_4tu1_user;Password=KDhmX5sVnKjzObdJK4F30u3zuloVtA15;SSL Mode=Require;Trust Server Certificate=true;";
var rawConnString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
// Use Render PostgreSQL if env var is empty or still points to Neon
var connString = (string.IsNullOrWhiteSpace(rawConnString) || rawConnString.Contains("neon.tech") || rawConnString.Contains("localhost"))
    ? RENDER_PG
    : rawConnString;
// Strip channel_binding from any URI-format connection string
if (connString.StartsWith("postgresql://") || connString.StartsWith("postgres://"))
{
    var uri = new Uri(connString.Split('?')[0]);
    var queryParams = connString.Contains('?') ? connString.Split('?')[1] : "";
    var filteredParams = string.Join("&", queryParams.Split('&')
        .Where(p => !p.StartsWith("channel_binding", StringComparison.OrdinalIgnoreCase)));
    var userInfo = uri.UserInfo.Split(':');
    connString = $"Host={uri.Host};Database={uri.AbsolutePath.TrimStart('/')};Username={Uri.UnescapeDataString(userInfo[0])};Password={Uri.UnescapeDataString(userInfo[1])};SSL Mode=Require;Trust Server Certificate=true;";
    if (!string.IsNullOrEmpty(filteredParams)) connString += filteredParams;
}
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connString);
var dataSource = dataSourceBuilder.Build();

// Register DbContext
builder.Services.AddDbContext<FinVedaDbContext>(options =>
{
    options.UseNpgsql(dataSource)
           .UseSnakeCaseNamingConvention();
});

// Configure CQRS (MediatR) & FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "FinVeda_Super_Secret_Key_1234567890";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddHttpContextAccessor();

// Configure RBAC
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Register AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Register Logging Services
builder.Services.AddScoped<IFileLoggerService, FileLoggerService>();
builder.Services.AddScoped<IControllerFileLoggerService, ControllerFileLoggerService>();
builder.Services.AddScoped<IControllerLogAnalyzerService, ControllerLogAnalyzerService>();

// Add Redis multiplexer with graceful fallback
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
    
    // Add abortConnect=false if not already present
    if (!redisConnectionString.Contains("abortConnect"))
    {
        redisConnectionString += ",abortConnect=false";
    }
    
    try
    {
        var options = ConfigurationOptions.Parse(redisConnectionString);
        options.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(options);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Redis connection warning: {ex.Message}. Continuing without Redis...");
        // Return a null multiplexer that won't crash the app
        return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
    }
});

// Register Services and Repositories
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<ILoanScheduleService, LoanScheduleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILoanCodeService, LoanCodeService>();
builder.Services.AddScoped<ILoanCaseService, LoanCaseService>();
builder.Services.AddScoped<IInstallmentService, InstallmentService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<ICapitalAccountService, CapitalAccountService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ILoanProductService, LoanProductService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IRecoveryService, RecoveryService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Phase 5: Register Double-Entry Accounting Services
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IAccountMappingService, AccountMappingService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IFinancialStatementService, FinancialStatementService>();
builder.Services.AddScoped<IReconciliationService, ReconciliationServiceImpl>();

// Phase 6: Register Month-End Close Services
builder.Services.AddScoped<MonthEndCloseService>();
builder.Services.AddScoped<AccrualService>();
builder.Services.AddScoped<ProvisionService>();
builder.Services.AddScoped<ProfitLossService>();
builder.Services.AddScoped<CashFlowService>();
builder.Services.AddScoped<BankReconciliationService>();
builder.Services.AddScoped<LoanLedgerReconciliationService>();
builder.Services.AddScoped<CollectionReconciliationService>();

// Phase 7: Register Interest Accounting Services
builder.Services.AddScoped<InterestCalculationService>();
builder.Services.AddScoped<InterestPostingService>();
builder.Services.AddScoped<InterestWaiverService>();
builder.Services.AddScoped<InterestReportingService>();

// Phase 8: Register P&L Statement Services
builder.Services.AddScoped<IProfitLossService, ProfitLossService>();
builder.Services.AddScoped<IRevenueTrackingService, RevenueTrackingService>();
builder.Services.AddScoped<IExpenseTrackingService, ExpenseTrackingService>();
builder.Services.AddScoped<IProfitLossReportingService, ProfitLossReportingService>();

// Phase 9: Register Cash Flow Services
builder.Services.AddScoped<ICashFlowService, CashFlowService>();
builder.Services.AddScoped<IOperatingCashFlowService, OperatingCashFlowService>();
builder.Services.AddScoped<IInvestingCashFlowService, InvestingCashFlowService>();
builder.Services.AddScoped<IFinancingCashFlowService, FinancingCashFlowService>();
builder.Services.AddScoped<ICashFlowForecastingService, CashFlowForecastingService>();
builder.Services.AddScoped<ILiquidityAnalysisService, LiquidityAnalysisService>();

// Phase 10: Register Advanced Services
builder.Services.AddScoped<IRiskManagementService, RiskManagementService>();
builder.Services.AddScoped<IAdvancedReportingService, AdvancedReportingService>();

// Phase 11: Register Compliance Services
builder.Services.AddScoped<IComplianceReportingService, ComplianceReportingService>();

// Phase 12: Register Multi-Currency Services
builder.Services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();
builder.Services.AddScoped<IMultiCurrencyLoanService, MultiCurrencyLoanService>();

// Phase 13: Register API Marketplace Services
builder.Services.AddScoped<IApiMarketplaceService, ApiMarketplaceService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IRateProviderService, RateProviderService>();

// Company Settings Service - Removed temporarily
// builder.Services.AddScoped<ICompanySettingsService, CompanySettingsService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IRedisLockService, RedisLockService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ICollectionRequestService, CollectionRequestService>();
builder.Services.AddScoped<ILoanClosureService, LoanClosureService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IEquityService, EquityService>();
builder.Services.AddScoped<IReceiptPdfService, ReceiptPdfService>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Enable Swagger in all environments (behind reverse proxy)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinVeda API v1"));

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowFrontend");

app.UseStaticFiles();

// HTTPS redirect disabled - running behind NGINX reverse proxy
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run pending SQL migrations at startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Fintech.Infrastructure.Persistence.FinVedaDbContext>();
    var migDir = Path.Combine(AppContext.BaseDirectory, "Migrations");
    if (Directory.Exists(migDir))
    {
        foreach (var sqlFile in Directory.GetFiles(migDir, "0*.sql").OrderBy(f => f))
        {
            try
            {
                var sql = await File.ReadAllTextAsync(sqlFile);
                await db.Database.ExecuteSqlRawAsync(sql);  
                Console.WriteLine($"✅ Applied migration: {Path.GetFileName(sqlFile)}");
            }
            catch (Exception ex)
            {
                // Idempotent migrations may throw "already exists" — that's okay
                Console.WriteLine($"ℹ️ Migration {Path.GetFileName(sqlFile)}: {ex.Message.Split('\n')[0]}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Migration runner error: {ex.Message}");
}

app.Run();
