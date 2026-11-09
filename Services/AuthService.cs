using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Crud.Dtos;
using Crud.Exceptions;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.AspNetCore.Identity;

namespace Crud.Services
{
    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly RefreshTokenService _refreshTokenService;

        public AuthService(
            IAuthRepository authRepository,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration,
            IRefreshTokenRepository refreshTokenRepository,
            RefreshTokenService refreshTokenService,
            ILogger<AuthService> logger
        )
        {
            _authRepository = authRepository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            throw new NotImplementedException();
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _authRepository.GetUserByUsernameAsync(loginDto.Username);

            if (user == null)
            {
                throw new InvalidCredentialsException();
            }
            if (!user.IsActive)
            {
                throw new InactiveAccountException();
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                loginDto.Password
            );
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new InvalidCredentialsException();
            }
            //i am going to generate a JWT token here and return it to the user
            var claims = new List<Claim>
            {
                // "sub" should hold the unique user identifier (usually the user ID)
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                // "email" is standard
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                // optional: include username as "unique_name"
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                //new Claim("permission", "Products.View") it was the first
            };
            if (user.Role == "Admin")
            {
                claims.Add(new Claim("permission", "Products.View"));
            }
            if (user.Role == "Admin")
            {
                claims.Add(new Claim("CanDeleteProduct", "true"));
            }
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing from configuration.");
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)
            );
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
            );
            //i need to get the expires date from the configuration file and use it to set the expiration of the token
            var durationInMinutes = Convert.ToDouble(_configuration["Jwt:DurationInMinutes"]);

            var expires = DateTime.UtcNow.AddMinutes(durationInMinutes);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = _refreshTokenService.GenerateRefreshToken();
            var refreshTokenRecord = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _refreshTokenService.HashToken(refreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };
            await _refreshTokenRepository.AddAsync(refreshTokenRecord);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expires,
            };
        }

        private async Task<RefreshToken> ValidateRefreshTokenAsync(string refreshToken)
        {
            var hashedToken = _refreshTokenService.HashToken(refreshToken);
            var storedRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(hashedToken);

            if (storedRefreshToken == null || storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidRefreshTokenException();
            }
            if (storedRefreshToken.RevokedAt != null)
            {
                throw new InactiveAccountException();
            }

            return storedRefreshToken;
        }

        //

        private (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
            };

            if (user.Role == "Admin")
            {
                claims.Add(new Claim("permission", "Products.View"));
                claims.Add(new Claim("CanDeleteProduct", "true"));
            }

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing from configuration.");
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
            );

            var durationInMinutes = Convert.ToDouble(_configuration["Jwt:DurationInMinutes"]);

            var expires = DateTime.UtcNow.AddMinutes(durationInMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return (accessToken, expires);
        }

        public async Task<LoginResponseDto> RefreshAsync(RefreshRequestDto request)
        {
            var storedToken = await ValidateRefreshTokenAsync(request.RefreshToken);
            var user = await _authRepository.GetUserByIdAsync(storedToken.UserId);
            if (user == null)
            {
                throw new UserNotFoundException();
            }
            if (!user.IsActive)
            {
                throw new InactiveAccountException();
            }
            var (newAccessToken, newExpiresAt) = GenerateAccessToken(user);
            var now = DateTime.UtcNow;
            //refresh token rotation: generate a new refresh token, hash it, store it, and revoke the old one
            var newRefreshToken = _refreshTokenService.GenerateRefreshToken();
            var NewRefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);
            var newRefreshTokenRecord = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = NewRefreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };
            var oldTokenHash = _refreshTokenService.HashToken(request.RefreshToken);
            var rotationSuccess = await _refreshTokenRepository.RotateAsync(
                storedToken.Id,
                oldTokenHash,
                now,
                NewRefreshTokenHash,
                newRefreshTokenRecord
            );
            if (!rotationSuccess)
            {
                throw new InvalidOperationException(
                    "Failed to rotate refresh token. Please log in again."
                );
            }

            return new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = newExpiresAt,
            };
        }

        public async Task RegisterAsync(RegisterDto registerDto)
        {
            bool userExists = await _authRepository.UserExistsAsync(registerDto.Username);
            if (userExists)
            {
                throw new InvalidOperationException("User already exists");
            }
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Role = "User",
                //Password = registerDto.Password // In a real application, you should hash the password before storing it
                //i want to hash the password before storing it
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);
            await _authRepository.RegisterAsync(user);
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            throw new NotImplementedException();
        }
    }
}
