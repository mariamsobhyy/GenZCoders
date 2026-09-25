using GenZCoders.Controllers;
using GenZCoders.Extensions;
using Microsoft.AspNetCore.Authorization;
using GenZCoders.Models;
using GenZCoders.Repos.AccountRoleRepo;
using GenZCoders.Repos.ApplicationRepo;
using GenZCoders.Repos.AuthRepo;
using GenZCoders.Repos.CourseMaterialRepo;
using GenZCoders.Repos.CourseRepo;
using GenZCoders.Repos.CourseRoundRepo;
using GenZCoders.Repos.CourseRoundInstructorRepo;
using GenZCoders.Repos.CourseRoundAssignmentRepo;
using GenZCoders.Repos.CourseRoundAssignmentSubmissionRepo;
using GenZCoders.Repos.EngineerDashboardRepo;
using GenZCoders.Repos.ExamRepo;
using GenZCoders.Repos.LoginRepo;
using GenZCoders.Repos.MediaRepo;
using GenZCoders.Repos.StudentExtensionRepo;
using GenZCoders.Repos.WeekRepo;
using GenZCoders.Services;
using GenZCoders.Services.AccountService;
using GenZCoders.Services.ApplicationService;
using GenZCoders.Services.AuthService;
using GenZCoders.Services.CourseMaterialService;
using GenZCoders.Services.CourseRoundService;
using GenZCoders.Services.CourseRoundInstructorService;
using GenZCoders.Services.CourseRoundAssignmentService;
using GenZCoders.Services.CourseRoundAssignmentSubmissionService;
using GenZCoders.Services.CourseService;
using GenZCoders.Services.EngineerDashboardService;
using GenZCoders.Services.MediaService;
using GenZCoders.Services.WeekService;
using GenZCoders.Services.Zoom;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GenZCoders.Repos.AdminDashboardRepo;
using GenZCoders.Services.AdminDashboardService;


var builder = WebApplication.CreateBuilder(args);

var connectionString =
    "Server=HAE5CG134101S;Database=ElsewedySchoolSysDB_DEV;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(connectionString));

Console.WriteLine("===== FORCED DATABASE =====");
Console.WriteLine(connectionString);
Console.WriteLine("===========================");

Console.WriteLine("JWT KEY = " + builder.Configuration["Jwt:Key"]);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Console.WriteLine("===== ALL CONNECTION CONFIG =====");

foreach (var item in builder.Configuration.AsEnumerable())
{
    if (item.Key.Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"{item.Key} = {item.Value}");
    }
}




Console.WriteLine("===== CONNECTION STRING =====");
Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));
Console.WriteLine("=============================");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // Tokens live for 2 hours; reject them as soon as the account is deactivated.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<SchoolDbContext>();
                var isActive = long.TryParse(context.Principal?.FindFirst("AccountId")?.Value, out var accountId)
                    && await db.Accounts.AnyAsync(a => a.Id == accountId && a.IsActive);

                if (!isActive)
                    context.Fail("Account is inactive.");
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Every endpoint requires a logged-in user unless it is marked [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(ClaimsPrincipalExtensions.StaffPolicy, policy =>
        policy.RequireAssertion(context => context.User.IsStaff()));

    options.AddPolicy(ClaimsPrincipalExtensions.ManagerPolicy, policy =>
        policy.RequireAssertion(context => context.User.IsManager()));
});
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountRepo, AccountRepo>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICourseRepo, CourseRepo>();
builder.Services.AddScoped<ILoginRepo, LoginRepo>();
builder.Services.AddScoped<IAccountRoleRepo, AccountRoleRepo>();
builder.Services.AddScoped<ICourseRoundAssignmentRepo, CourseRoundAssignmentRepo>();
builder.Services.AddScoped<ICourseRoundAssignmentSubmissionRepo, CourseRoundAssignmentSubmissionRepo>();
builder.Services.AddScoped<ICourseRoundAssignmentService, CourseRoundAssignmentService>();
builder.Services.AddScoped<ICourseRoundAssignmentSubmissionService, CourseRoundAssignmentSubmissionService>();
builder.Services.AddScoped<ICourseRoundRepo, CourseRoundRepo>();
builder.Services.AddScoped<ICourseRoundService, CourseRoundService>();
builder.Services.AddScoped<IApplicationRepo, ApplicationRepo>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IStudentExtensionRepo, StudentExtensionRepo>();
builder.Services.AddScoped<IWeekRepo, WeekRepo>();
builder.Services.AddScoped<IWeekService, WeekService>();
builder.Services.AddScoped<ICourseMaterialService, CourseMaterialService>();
builder.Services.AddScoped<ICourseMaterialRepo, CourseMaterialRepo>();
builder.Services.AddScoped<ICourseRoundInstructorRepository, CourseRoundInstructorRepository>();
builder.Services.AddScoped<ICourseRoundInstructorService, CourseRoundInstructorService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IAccountRoleRepo, AccountRoleRepo>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEngineerDashboardRepo, EngineerDashBoardRepo>();
builder.Services.AddScoped<IEngineerDashboardService, EngineerDashboardService>();
builder.Services.AddScoped<IAdminDashboardRepo, AdminDashboardRepo>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddHttpClient<IZoomService, ZoomService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IExamQuestionBankRepo, ExamQuestionBankRepo>();
builder.Services.AddScoped<IExamQuestionRepo, ExamQuestionRepo>();
builder.Services.AddScoped<IStudentExamAnswerRepo, StudentExamAnswerRepo>();



// Only the frontend origins listed in config (Cors:AllowedOrigins) may call the API from a browser.
// Falls back to the local Vite dev server when nothing is configured.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is { Length: > 0 } origins
    ? origins
    : new[] { "http://localhost:3039" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Logging.AddConsole();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        if (exception is not null)
        {
            app.Logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            System.ArgumentException => StatusCodes.Status400BadRequest,
            System.InvalidOperationException => StatusCodes.Status409Conflict,
            System.UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            System.Collections.Generic.KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };

        // Known exception types carry messages meant for the user; anything else may expose
        // internals (SQL, stack details), so outside development it gets a generic message.
        var showMessage = context.Response.StatusCode != StatusCodes.Status500InternalServerError
            || app.Environment.IsDevelopment();

        await context.Response.WriteAsJsonAsync(new
        {
            message = showMessage && exception is not null
                ? exception.Message
                : "An unexpected error occurred."
        });
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();

    Console.WriteLine("===== DATABASE DEBUG =====");
    Console.WriteLine($"Server: {db.Database.GetDbConnection().DataSource}");
    Console.WriteLine($"Database: {db.Database.GetDbConnection().Database}");
    Console.WriteLine("==========================");
}
app.Run();
