using System.Security.Claims;
using Application.Mappings;
using Application.UseCases.Knowledges.LearningLists;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Constants;

namespace UnitTests.Knowledges.LearningLists
{
    public class UpdateLearningListUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<LearningList>> _learningListRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly IMapper _mapper;
        private readonly UpdateLearningListUseCase _updateLearningListUseCase;

        public UpdateLearningListUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _learningListRepositoryMock = new Mock<IRepository<LearningList>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _unitOfWorkMock.Setup(u => u.Repository<LearningList>()).Returns(_learningListRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_userRepositoryMock.Object);

            _updateLearningListUseCase = new UpdateLearningListUseCase(_unitOfWorkMock.Object, _mapper, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            // Arrange
            var parameters = new UpdateLearningListParams
            {
                Id = Guid.NewGuid(),
                Title = "Updated Learning List"
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns((Claim?)null);

            // Act
            var result = await _updateLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenLearningListNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new UpdateLearningListParams
            {
                Id = Guid.NewGuid(),
                Title = "Updated Learning List"
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.GetById(parameters.Id)).ReturnsAsync((LearningList?)null);

            // Act
            var result = await _updateLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.NoLearningListFoundWithGuid, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotAuthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new UpdateLearningListParams
            {
                Id = Guid.NewGuid(),
                Title = "Updated Learning List"
            };

            var learningList = new LearningList
            {
                Id = parameters.Id,
                Title = "Old Learning List",
                LearnerId = Guid.NewGuid()
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.GetById(parameters.Id)).ReturnsAsync(learningList);

            // Act
            var result = await _updateLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotAuthorized, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenLearningListTitleExisted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new UpdateLearningListParams
            {
                Id = Guid.NewGuid(),
                Title = "Old Learning List"
            };

            var learningList = new LearningList
            {
                Id = parameters.Id,
                Title = "Old Learning List",
                LearnerId = userId
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.GetById(parameters.Id)).ReturnsAsync(learningList);
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ReturnsAsync(learningList);

            // Act
            var result = await _updateLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.LearningListTitleExisted, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenLearningListIsUpdated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new UpdateLearningListParams
            {
                Id = Guid.NewGuid(),
                Title = "Updated Learning List"
            };

            var learningList = new LearningList
            {
                Id = parameters.Id,
                Title = "Old Learning List",
                LearnerId = userId
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.GetById(parameters.Id)).ReturnsAsync(learningList);
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ReturnsAsync((LearningList?)null);
            _learningListRepositoryMock.Setup(r => r.Update(It.IsAny<LearningList>())).ReturnsAsync(learningList);

            // Act
            var result = await _updateLearningListUseCase.Execute(parameters);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(learningList.Id, result.Value.Id);
            Assert.Equal(parameters.Title, result.Value.Title);
        }
    }
}
