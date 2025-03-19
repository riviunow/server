using AutoMapper;
using Moq;

using Application.DTOs;
using Application.UseCases.Auth;
using Domain.Base;
using Domain.Interfaces;
using Shared.Constants;
using Shared.Utils;
using Domain.Entities.SingleIdEntities;
using Shared.Config;
using Microsoft.Extensions.Options;
using Application.UseCases.JWT;
using Microsoft.Extensions.Configuration;

namespace UnitTests.Auth
{
    public class LoginUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IOptions<JwtSettings>> _jwtOptionsMock;
        private readonly GenerateTokenPairUseCase _generateTokenPairUseCase;
        private readonly Mock<IConfiguration> _configurationMock;

        private readonly LoginUseCase _loginUseCase;

        public LoginUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _mapperMock = new Mock<IMapper>();
            _jwtOptionsMock = new Mock<IOptions<JwtSettings>>();
            _jwtOptionsMock.Setup(o => o.Value).Returns(new JwtSettings
            {
                SecretKey = "test_secret_key_must_have_at_least_16_chars",
                Issuer = "test_issuer",
                Audience = "test_audience",
                ExpiryMinutes = 180
            });
            _configurationMock = new Mock<IConfiguration>();
            var Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _configurationMock.SetupGet(c => c["JwtSettings:RefreshTokenExpiryInDays"]).Returns(Configuration["JwtSettings:RefreshTokenExpiryInDays"]);
            _configurationMock.SetupGet(c => c["JwtSettings:AccessTokenExpiryInHours"]).Returns(Configuration["JwtSettings:AccessTokenExpiryInHours"]);

            _generateTokenPairUseCase = new GenerateTokenPairUseCase(_jwtOptionsMock.Object, _configurationMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_userRepositoryMock.Object);

            _loginUseCase = new LoginUseCase(_unitOfWorkMock.Object, _mapperMock.Object, _generateTokenPairUseCase, _configurationMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var parameters = new LoginParams { Email = "test@example.com", Password = "password" };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync((User?)null);

            var result = await _loginUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFoundWithEmail.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenPasswordIsWrong()
        {
            var parameters = new LoginParams { Email = "test@example.com", Password = "wrongpassword" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = PasswordHasher.HashWithSHA256("rightpassword"),
                    IsEmailConfirmed = true,
                    IsActivated = true
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);
            user.Authentication!.VerifyPassword(parameters.Password).Equals(false);

            var result = await _loginUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.WrongPassword.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenEmailNotConfirmed()
        {
            var parameters = new LoginParams { Email = "test@example.com", Password = "password" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = PasswordHasher.HashWithSHA256(parameters.Password),
                    IsEmailConfirmed = false,
                    IsActivated = true
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);

            var result = await _loginUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.EmailNotConfirmed.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenAccountIsLocked()
        {
            var parameters = new LoginParams { Email = "test@example.com", Password = "password" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = PasswordHasher.HashWithSHA256(parameters.Password),
                    IsEmailConfirmed = true,
                    IsActivated = false
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);

            var result = await _loginUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.AccountIsLocked.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenLoginIsSuccessful()
        {
            var parameters = new LoginParams { Email = "test@example.com", Password = "password" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = PasswordHasher.HashWithSHA256(parameters.Password),
                    IsEmailConfirmed = true,
                    IsActivated = true
                }
            };
            var _authenticationRepositoryMock = new Mock<IRepository<Authentication>>();
            _unitOfWorkMock.Setup(u => u.Repository<Authentication>()).Returns(_authenticationRepositoryMock.Object);

            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Email = user.Email, UserName = user.UserName });

            var result = await _loginUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Item1);
            Assert.NotNull(result.Value.Item2);
            Assert.Equal(user.Email, result.Value.Item1.Email);
            Assert.Equal(user.UserName, result.Value.Item1.UserName);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenExceptionIsThrown()
        {
            var parameters = new LoginParams { Email = "test@example.com", Password = "password" };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ThrowsAsync(new Exception());

            var result = await _loginUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UnknownError.ToString(), result.Errors[0]);
        }
    }
}