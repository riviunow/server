using System.Security.Claims;
using Application.Mappings;
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
    public class DeleteAccountUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IMapper _mapper;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly DeleteAccountUseCase _deleteAccountUseCase;

        public DeleteAccountUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(); _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _deleteAccountUseCase = new DeleteAccountUseCase(_unitOfWorkMock.Object, _mapper, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenUserIsDeleted()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "testuser", Email = "testuser@example.com" };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().GetById(userId)).ReturnsAsync(user);
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().Delete(user)).ReturnsAsync(user);

            var result = await _deleteAccountUseCase.Execute(NoParam.Value);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _unitOfWorkMock.Setup(uow => uow.Repository<User>().GetById(userId)).ReturnsAsync((User?)null);

            var result = await _deleteAccountUseCase.Execute(NoParam.Value);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFound.ToString(), result.Errors[0]);
        }
    }
}