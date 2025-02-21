using Application.DTOs;
using Application.UseCases.Auth;
using Application.UseCases.JWT;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Config;
using Shared.Constants;

namespace UnitTests.Auth
{
    public class ConfirmPasswordResettingEmailUseCaseTest
    {
        private readonly Mock<IRepository<Authentication>> _authenticationRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IOptions<JwtSettings>> _jwtOptionsMock;
        private readonly GenerateTokenPairUseCase _generateTokenPairUseCase;
        private readonly ConfirmPasswordResettingEmailUseCase _confirmPasswordResettingEmailUseCase;
        private readonly Mock<IConfiguration> _configurationMock;

        public ConfirmPasswordResettingEmailUseCaseTest()
        {
            _authenticationRepositoryMock = new Mock<IRepository<Authentication>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _jwtOptionsMock = new Mock<IOptions<JwtSettings>>();
            _jwtOptionsMock.Setup(o => o.Value).Returns(new JwtSettings
            {
                SecretKey = "test_secret_key_must_have_at_least_16_chars",
                Issuer = "test_issuer",
                Audience = "test_audience",
                ExpiryMinutes = 180
            });

            _unitOfWorkMock.Setup(u => u.Repository<Authentication>()).Returns(_authenticationRepositoryMock.Object);

            _configurationMock = new Mock<IConfiguration>();
            var Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _configurationMock.SetupGet(c => c["JwtSettings:RefreshTokenExpiryInDays"]).Returns(Configuration["JwtSettings:RefreshTokenExpiryInDays"]);
            _configurationMock.SetupGet(c => c["JwtSettings:AccessTokenExpiryInHours"]).Returns(Configuration["JwtSettings:AccessTokenExpiryInHours"]);

            _generateTokenPairUseCase = new GenerateTokenPairUseCase(_jwtOptionsMock.Object, _configurationMock.Object);
            _confirmPasswordResettingEmailUseCase = new ConfirmPasswordResettingEmailUseCase(_unitOfWorkMock.Object, _mapperMock.Object, _generateTokenPairUseCase, _configurationMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenConfirmationIsSuccessful()
        {
            var parameters = new ConfirmPasswordResettingEmailParams
            {
                Email = "test@example.com",
                ConfirmationCode = "validCode",
                Password = "newPassword"
            };
            var user = new User
            {
                UserName = "testuser",
                Email = parameters.Email,
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsEmailConfirmed = true,
                    ConfirmationCode = parameters.ConfirmationCode,
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10),
                    IsActivated = true
                }
            };

            var userRepositoryMock = new Mock<IRepository<User>>();
            userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(userRepositoryMock.Object);
            _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Email = user.Email, UserName = user.UserName });

            var result = await _confirmPasswordResettingEmailUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Item1);
            Assert.NotNull(result.Value.Item2);
            Assert.Equal(user.Email, result.Value.Item1.Email);
            Assert.Null(user.Authentication);
            _authenticationRepositoryMock.Verify(r => r.Update(It.IsAny<Authentication>()), Times.Once);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var parameters = new ConfirmPasswordResettingEmailParams
            {
                Email = "test@example.com",
                ConfirmationCode = "validCode",
                Password = "newPassword"
            };

            var userRepositoryMock = new Mock<IRepository<User>>();
            userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>())).ReturnsAsync((User?)null);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(userRepositoryMock.Object);

            var result = await _confirmPasswordResettingEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFoundWithEmail.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenConfirmationCodeIsInvalid()
        {
            var parameters = new ConfirmPasswordResettingEmailParams
            {
                Email = "test@example.com",
                ConfirmationCode = "invalidCode",
                Password = "newPassword"
            };
            var user = new User
            {
                UserName = "testuser",
                Email = parameters.Email,
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsEmailConfirmed = true,
                    ConfirmationCode = "validCode",
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10),
                    IsActivated = true
                }
            };

            var userRepositoryMock = new Mock<IRepository<User>>();
            userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(userRepositoryMock.Object);

            var result = await _confirmPasswordResettingEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.InvalidConfirmationCode.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenConfirmationCodeIsExpired()
        {
            var parameters = new ConfirmPasswordResettingEmailParams
            {
                Email = "test@example.com",
                ConfirmationCode = "validCode",
                Password = "newPassword"
            };
            var user = new User
            {
                UserName = "testuser",
                Email = parameters.Email,
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsEmailConfirmed = true,
                    ConfirmationCode = parameters.ConfirmationCode,
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(-10),
                    IsActivated = true
                }
            };

            var userRepositoryMock = new Mock<IRepository<User>>();
            userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(userRepositoryMock.Object);

            var result = await _confirmPasswordResettingEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.ConfirmationCodeExpired.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenAccountIsLocked()
        {
            var parameters = new ConfirmPasswordResettingEmailParams
            {
                Email = "test@example.com",
                ConfirmationCode = "validCode",
                Password = "newPassword"
            };
            var user = new User
            {
                UserName = "testuser",
                Email = parameters.Email,
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsEmailConfirmed = true,
                    ConfirmationCode = parameters.ConfirmationCode,
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10),
                    IsActivated = false // Account is locked
                }
            };

            var userRepositoryMock = new Mock<IRepository<User>>();
            userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>())).ReturnsAsync(user);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(userRepositoryMock.Object);

            var result = await _confirmPasswordResettingEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.AccountIsLocked.ToString(), result.Errors[0]);
        }
    }
}
