using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Profile;
using AutoMapper;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Constants;

namespace UnitTests.Profile
{
    public class UpdateProfileUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IFileStorageService> _fileStorageServiceMock;
        private readonly UpdateProfileUseCase _updateProfileUseCase;

        public UpdateProfileUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _fileStorageServiceMock = new Mock<IFileStorageService>();
            _updateProfileUseCase = new UpdateProfileUseCase(_unitOfWorkMock.Object, _mapperMock.Object, _httpContextAccessorMock.Object, _fileStorageServiceMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenProfileIsUpdated()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "testuser", Email = "testuser@example.com" };
            var updateParams = new UpdateProfileParams { UserName = "newusername", Photo = null };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().GetById(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().Update(user)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Id = user.Id, UserName = user.UserName, Email = user.Email });

            var result = await _updateProfileUseCase.Execute(updateParams);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            var updateParams = new UpdateProfileParams { UserName = "newusername", Photo = null };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().GetById(userId)).ReturnsAsync((User?)null);

            var result = await _updateProfileUseCase.Execute(updateParams);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFound.ToString(), result.Errors[0]);
        }
    }
}