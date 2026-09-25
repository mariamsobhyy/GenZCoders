using GenZCoders.Extensions;
using Microsoft.AspNetCore.Authorization;
using GenZCoders.DTOs.AdminDto;
using GenZCoders.Models;
using GenZCoders.Services.AuthService;
using Microsoft.AspNetCore.Mvc;
using GenZCoders.Services;
using GenZCoders.Services.AdminDashboardService;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = ClaimsPrincipalExtensions.ManagerPolicy)]
    public class AdminController : ControllerBase
    {
        private readonly SchoolDbContext _context;
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminController(
            SchoolDbContext context,
            IAdminDashboardService adminDashboardService)
        {
            _context = context;
            _adminDashboardService = adminDashboardService;
        }

        // ============================================================
        // CREATE INSTRUCTOR
        // ============================================================

        [HttpPost("create-instructor")]
        public async Task<IActionResult> CreateInstructor(
            [FromBody] CreateUserRequestDto dto)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Id == 37 &&
                    r.RoleName == "co-Instructor" &&
                    r.BusinessEntity == "GenZCoders");

            if (role == null)
            {
                return BadRequest(new
                {
                    message = "GenZCoders co-Instructor role was not found."
                });
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            var nationalId = dto.NationalId.Trim();

            if (await _context.Accounts.AnyAsync(a => a.Email == email))
            {
                return BadRequest(new
                {
                    message = "An account with this email already exists."
                });
            }

            if (await _context.Accounts.AnyAsync(a => a.NationalId == nationalId))
            {
                return BadRequest(new
                {
                    message = "An account with this national ID already exists."
                });
            }

            var account = new Account
            {
                FullNameEn = dto.FullNameEn.Trim(),
                FullNameAr = dto.FullNameAr?.Trim() ?? "",
                NationalId = nationalId,
                Phone = dto.Phone?.Trim(),
                Email = email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                RoleId = role.Id,
                StatusId = 1,
                IsActive = true,
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Accounts.Add(account);

            await _context.SaveChangesAsync();

            var accountRole = new AccountRole
            {
                AccountId = account.Id,
                RoleId = role.Id,
                BusinessEntityName = "GenZCoders"
            };

            _context.AccountRoles.Add(accountRole);
            await SyncLoginAsync(account, account.PasswordHash);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Instructor created successfully.",
                id = account.Id,
                email = account.Email
            });
        }

        // ============================================================
        // CREATE ADMIN
        // ============================================================

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin(
            [FromBody] CreateUserRequestDto dto)
        {
            return BadRequest(new
            {
                message = "There is currently no Admin role configured for GenZCoders in the database."
            });
        }

        // ============================================================
        // CREATE GENERAL GENZCODERS USER
        // ============================================================

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateGenZCodersUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    message = "Password is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.FullNameEn))
            {
                return BadRequest(new
                {
                    message = "English name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.NationalId))
            {
                return BadRequest(new
                {
                    message = "National ID is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.RoleName))
            {
                return BadRequest(new
                {
                    message = "Role is required."
                });
            }

            var roleName = dto.RoleName.Trim();

            // Admin is not configured for GenZCoders
            if (roleName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "There is currently no Admin role configured for GenZCoders."
                });
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.BusinessEntity == "GenZCoders" &&
                    r.RoleName == roleName);

            if (role == null)
            {
                return BadRequest(new
                {
                    message = $"Role '{roleName}' was not found for GenZCoders."
                });
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            var nationalId = dto.NationalId.Trim();

            if (await _context.Accounts.AnyAsync(a => a.Email == email))
            {
                return BadRequest(new
                {
                    message = "An account with this email already exists."
                });
            }

            if (await _context.Accounts.AnyAsync(a => a.NationalId == nationalId))
            {
                return BadRequest(new
                {
                    message = "An account with this national ID already exists."
                });
            }

            var account = new Account
            {
                FullNameEn = dto.FullNameEn.Trim(),
                FullNameAr = dto.FullNameAr?.Trim() ?? "",
                NationalId = nationalId,
                Phone = dto.Phone?.Trim(),
                Email = email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                RoleId = role.Id,
                StatusId = 1,
                IsActive = true,
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Accounts.Add(account);

            await _context.SaveChangesAsync();

            var accountRole = new AccountRole
            {
                AccountId = account.Id,
                RoleId = role.Id,
                BusinessEntityName = "GenZCoders"
            };

            _context.AccountRoles.Add(accountRole);
            await SyncLoginAsync(account, account.PasswordHash);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"{role.RoleName} created successfully.",
                id = account.Id,
                email = account.Email
            });
        }

        // ============================================================
        // UPDATE USER
        // ============================================================

        [HttpPut("users/{id:long}")]
        public async Task<IActionResult> UpdateUser(
            long id,
            [FromBody] UpdateUserRequestDto dto)
        {
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Role.BusinessEntity == "GenZCoders");

            if (account == null)
            {
                return NotFound(new
                {
                    message = "GenZCoders user was not found."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message = "Email is required."
                });
            }

            var email = dto.Email.Trim().ToLowerInvariant();

            var emailExists = await _context.Accounts
                .AnyAsync(a =>
                    a.Email == email &&
                    a.Id != id);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "An account with this email already exists."
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.NationalId))
            {
                var nationalId = dto.NationalId.Trim();

                var nationalIdExists = await _context.Accounts
                    .AnyAsync(a =>
                        a.NationalId == nationalId &&
                        a.Id != id);

                if (nationalIdExists)
                {
                    return BadRequest(new
                    {
                        message = "An account with this national ID already exists."
                    });
                }

                account.NationalId = nationalId;
            }

            account.Email = email;

            if (!string.IsNullOrWhiteSpace(dto.FullNameEn))
            {
                account.FullNameEn = dto.FullNameEn.Trim();
            }

            // Only overwrite optional fields the client actually sent.
            if (dto.FullNameAr != null)
            {
                account.FullNameAr = dto.FullNameAr.Trim();
            }

            if (dto.Phone != null)
            {
                account.Phone = dto.Phone.Trim();
            }

            account.IsActive = dto.IsActive;

            // Update password only if user entered a new password
            string? newPasswordHash = null;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                newPasswordHash = PasswordHasher.Hash(dto.Password);
                account.PasswordHash = newPasswordHash;
            }

            await SyncLoginAsync(account, newPasswordHash);

            // Update role if provided
            if (!string.IsNullOrWhiteSpace(dto.RoleName))
            {
                var roleName = dto.RoleName.Trim();

                if (roleName.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message = "There is currently no Admin role configured for GenZCoders."
                    });
                }

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r =>
                        r.BusinessEntity == "GenZCoders" &&
                        r.RoleName == roleName);

                if (role == null)
                {
                    return BadRequest(new
                    {
                        message = $"Role '{roleName}' was not found for GenZCoders."
                    });
                }

                account.RoleId = role.Id;

                var existingAccountRole = await _context.AccountRoles
                    .FirstOrDefaultAsync(ar =>
                        ar.AccountId == account.Id &&
                        ar.BusinessEntityName == "GenZCoders");

                if (existingAccountRole != null)
                {
                    existingAccountRole.RoleId = role.Id;
                }
                else
                {
                    _context.AccountRoles.Add(new AccountRole
                    {
                        AccountId = account.Id,
                        RoleId = role.Id,
                        BusinessEntityName = "GenZCoders"
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User updated successfully.",
                id = account.Id
            });
        }

        // ============================================================
        // DELETE / DEACTIVATE USER
        // ============================================================

        [HttpDelete("users/{id:long}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Role.BusinessEntity == "GenZCoders");

            if (account == null)
            {
                return NotFound(new
                {
                    message = "GenZCoders user was not found."
                });
            }

            // Soft delete
            account.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User deactivated successfully.",
                id = account.Id
            });
        }

        // ============================================================
        // ACTIVATE USER
        // ============================================================

        [HttpPatch("users/{id:long}/activate")]
        public async Task<IActionResult> ActivateUser(long id)
        {
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Role.BusinessEntity == "GenZCoders");

            if (account == null)
            {
                return NotFound(new
                {
                    message = "GenZCoders user was not found."
                });
            }

            account.IsActive = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User activated successfully.",
                id = account.Id
            });
        }

        // ============================================================
        // LOGIN SYNC
        // ============================================================

        // AuthService signs users in through the Logins table, so every account
        // created or edited here needs a matching Login row with the same email.
        private async Task SyncLoginAsync(Account account, string? newPasswordHash)
        {
            var login = await _context.Logins.FirstOrDefaultAsync(l => l.AccountId == account.Id);

            if (login == null)
            {
                _context.Logins.Add(new Login
                {
                    AccountId = account.Id,
                    Email = account.Email,
                    PasswordHash = newPasswordHash ?? account.PasswordHash,
                    StatusId = 1
                });
                return;
            }

            login.Email = account.Email;
            if (newPasswordHash != null)
            {
                login.PasswordHash = newPasswordHash;
            }
        }

        // ============================================================
        // DASHBOARD
        // ============================================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _adminDashboardService.GetDashboardAsync();

            return Ok(result);
        }

        // ============================================================
        // GET ALL USERS
        // ============================================================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Status)
                .Where(a => a.Role.BusinessEntity == "GenZCoders")
                .Select(a => new UserInfoDto
                {
                    Id = a.Id,
                    Email = a.Email,
                    FullNameEn = a.FullNameEn,
                    FullNameAr = a.FullNameAr,
                    Phone = a.Phone,

                    RoleId = a.RoleId,
                    RoleName = a.Role.RoleName,

                    StatusId = a.StatusId,
                    StatusName = a.Status.StatusName,

                    IsActive = a.IsActive,
                    IsEmailVerified = false,
                    CreatedAt = a.CreatedAt,
                    LastLoginAt = null,
                    HasPassword = !string.IsNullOrEmpty(a.PasswordHash),
                    HasGoogleAuth = false
                })
                .ToListAsync();

            return Ok(users);
        }
    }

    // ================================================================
    // DTOs USED BY ADMIN CRUD
    // ================================================================

    public class CreateGenZCodersUserDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string FullNameEn { get; set; } = "";
        public string? FullNameAr { get; set; }
        public string? Phone { get; set; }
        public string RoleName { get; set; } = "";
    }

    public class UpdateUserRequestDto
    {
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string? NationalId { get; set; }
        public string FullNameEn { get; set; } = "";
        public string? FullNameAr { get; set; }
        public string? Phone { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; }
    }
}