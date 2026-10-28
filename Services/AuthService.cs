using Crud.Dtos;
using Crud.Exceptions;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Crud.Services
{
    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly RefreshTokenService _refreshTokenService;

        public AuthService(IAuthRepository authRepository, IPasswordHasher<User> passwordHasher, IConfiguration configuration,
            IRefreshTokenRepository refreshTokenRepository, RefreshTokenService refreshTokenService)
        {
            _authRepository = authRepository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenService = refreshTokenService;

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

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
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
                        new Claim(ClaimTypes.Role,user.Role),
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
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            //i need to get the expires date from the configuration file and use it to set the expiration of the token
            var durationInMinutes =
                Convert.ToDouble(_configuration["Jwt:DurationInMinutes"]);

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
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            await _refreshTokenRepository.AddAsync(refreshTokenRecord);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expires
            };
        }
        private async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
        {
            var hashedToken = _refreshTokenService.HashToken(refreshToken);
            var storedRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(hashedToken);

            if (storedRefreshToken == null || storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }

            return storedRefreshToken;
        }
        public Task<LoginResponseDto> RefreshAsync(RefreshRequestDto request)
        {
            throw new NotImplementedException();
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
                Role = "User"
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
