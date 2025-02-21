using System.Security.Claims;
using Application.DTOs;
using Application.UseCases.Profile;
using AutoMapper;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Constants;

namespace UnitTests.Profile
{
    public class GetProfileUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly GetProfileUseCase _getProfileUseCase;

        public GetProfileUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _getProfileUseCase = new GetProfileUseCase(_unitOfWorkMock.Object, _mapperMock.Object, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenUserIsFound()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "testuser", Email = "testuser@example.com" };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().GetById(userId)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Id = user.Id, UserName = user.UserName, Email = user.Email });

            var result = await _getProfileUseCase.Execute(NoParam.Value);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().GetById(userId)).ReturnsAsync((User?)null);

            var result = await _getProfileUseCase.Execute(NoParam.Value);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFound.ToString(), result.Errors[0]);
        }
    }
}