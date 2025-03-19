using AutoMapper;
using Moq;

using Application.DTOs;
using Application.UseCases.Auth;
using Domain.Base;
using Domain.Interfaces;
using Shared.Constants;
using Domain.Entities.SingleIdEntities;
using Shared.Config;
using Microsoft.Extensions.Options;
using Application.UseCases.JWT;
using Microsoft.Extensions.Configuration;

namespace UnitTests.Auth
{
    public class ConfirmRegistrationEmailUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IOptions<JwtSettings>> _jwtOptionsMock;
        private readonly GenerateTokenPairUseCase _generateTokenPairUseCase;
        private readonly ConfirmRegistrationEmailUseCase _confirmRegistrationEmailUseCase;
        private readonly Mock<IConfiguration> _configurationMock;

        public ConfirmRegistrationEmailUseCaseTest()
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

            _confirmRegistrationEmailUseCase = new ConfirmRegistrationEmailUseCase(_unitOfWorkMock.Object, _mapperMock.Object, _generateTokenPairUseCase, _configurationMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync((User?)null);

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFoundWithEmail.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenEmailAlreadyConfirmed()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsEmailConfirmed = true,
                    IsActivated = true,
                    ConfirmationCode = "123456",
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10)
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
            .ReturnsAsync(user);

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.EmailAlreadyConfirmed.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenConfirmationCodeIsNull()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsActivated = true,
                    IsEmailConfirmed = false,
                    ConfirmationCode = null,
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10)
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
            .ReturnsAsync(user);

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.EmailAlreadyConfirmed.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenConfirmationCodeExpiryTimeIsNull()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsActivated = true,
                    IsEmailConfirmed = false,
                    ConfirmationCode = "123456",
                    ConfirmationCodeExpiryTime = null
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
            .ReturnsAsync(user);

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.EmailAlreadyConfirmed.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenInvalidConfirmationCode()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "wrongcode" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsEmailConfirmed = false,
                    ConfirmationCode = "123456",
                    IsActivated = true,
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10)
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.InvalidConfirmationCode.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenConfirmationCodeExpired()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsActivated = true,
                    IsEmailConfirmed = false,
                    ConfirmationCode = "123456",
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(-10)
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.ConfirmationCodeExpired.ToString(), result.Errors[0]);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenConfirmationIsSuccessful()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            var user = new User
            {
                Email = parameters.Email,
                UserName = "testuser",
                Authentication = new Authentication
                {
                    UserId = Guid.NewGuid(),
                    HashedPassword = "hashedpassword",
                    IsActivated = true,
                    IsEmailConfirmed = false,
                    ConfirmationCode = "123456",
                    ConfirmationCodeExpiryTime = DateTime.UtcNow.AddMinutes(10)
                }
            };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ReturnsAsync(user);
            var _authenticationRepositoryMock = new Mock<IRepository<Authentication>>();
            _unitOfWorkMock.Setup(u => u.Repository<Authentication>()).Returns(_authenticationRepositoryMock.Object);

            _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto { Email = user.Email, UserName = user.UserName });

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Item1);
            Assert.NotNull(result.Value.Item2);
            Assert.Equal(user.Email, result.Value.Item1.Email);
            Assert.Equal(user.UserName, result.Value.Item1.UserName);
            Assert.Null(user.Authentication);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenExceptionIsThrown()
        {
            var parameters = new ConfirmRegistrationEmailParams { Email = "test@example.com", ConfirmationCode = "123456" };
            _userRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<User>>()))
                .ThrowsAsync(new Exception());

            var result = await _confirmRegistrationEmailUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UnknownError.ToString(), result.Errors[0]);
        }
    }
}