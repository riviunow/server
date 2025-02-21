using Application.UseCases.Knowledges.LearningLists;
using AutoMapper;
using Domain.Entities.SingleIdEntities;
using Domain.Entities.PivotEntities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Constants;
using System.Security.Claims;
using Application.Mappings;
using Domain.Base;

namespace UnitTests.Knowledges.LearningLists
{
    public class CreateLearningListUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<LearningList>> _learningListRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IRepository<Knowledge>> _knowledgeRepositoryMock;
        private readonly Mock<IRepository<LearningListKnowledge>> _learningListKnowledgeRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly IMapper _mapper;
        private readonly CreateLearningListUseCase _createLearningListUseCase;

        public CreateLearningListUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _learningListRepositoryMock = new Mock<IRepository<LearningList>>();
            _knowledgeRepositoryMock = new Mock<IRepository<Knowledge>>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _learningListKnowledgeRepositoryMock = new Mock<IRepository<LearningListKnowledge>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _unitOfWorkMock.Setup(u => u.Repository<LearningList>()).Returns(_learningListRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Knowledge>()).Returns(_knowledgeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<LearningListKnowledge>()).Returns(_learningListKnowledgeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_userRepositoryMock.Object);

            _createLearningListUseCase = new CreateLearningListUseCase(_unitOfWorkMock.Object, _mapper, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            // Arrange
            var parameters = new CreateLearningListParams
            {
                Title = "Test Learning List",
                KnowledgeIds = new List<Guid> { Guid.NewGuid() }
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns((Claim?)null);

            // Act
            var result = await _createLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenLearningListTitleExisted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new CreateLearningListParams
            {
                Title = "Test Learning List",
                KnowledgeIds = new List<Guid> { Guid.NewGuid() }
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ReturnsAsync(new LearningList { Title = parameters.Title });

            // Act
            var result = await _createLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.LearningListTitleExisted, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenLearningListIsCreated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new CreateLearningListParams
            {
                Title = "Test Learning List",
                KnowledgeIds = new List<Guid> { Guid.NewGuid() }
            };

            var learningList = new LearningList
            {
                Id = Guid.NewGuid(),
                Title = parameters.Title,
                LearnerId = userId
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ReturnsAsync((LearningList?)null);
            _learningListRepositoryMock.Setup(r => r.Add(It.IsAny<LearningList>())).ReturnsAsync(learningList);
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(new List<Knowledge> { new Knowledge { Title = "", Id = parameters.KnowledgeIds[0], Visibility = KnowledgeVisibility.Public } });

            // Act
            var result = await _createLearningListUseCase.Execute(parameters);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(learningList.Id, result.Value.Id);
            Assert.Equal(learningList.Title, result.Value.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenKnowledgeNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new CreateLearningListParams
            {
                Title = "Test Learning List",
                KnowledgeIds = new List<Guid> { Guid.NewGuid() }
            };

            var learningList = new LearningList
            {
                Id = Guid.NewGuid(),
                Title = parameters.Title,
                LearnerId = userId
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ReturnsAsync((LearningList?)null);
            _learningListRepositoryMock.Setup(r => r.Add(It.IsAny<LearningList>())).ReturnsAsync(learningList);
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(new List<Knowledge>());

            // Act
            var result = await _createLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.SomeKnowledgesNotFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotAuthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new CreateLearningListParams
            {
                Title = "Test Learning List",
                KnowledgeIds = new List<Guid> { Guid.NewGuid() }
            };

            var learningList = new LearningList
            {
                Id = Guid.NewGuid(),
                Title = parameters.Title,
                LearnerId = userId
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ReturnsAsync((LearningList?)null);
            _learningListRepositoryMock.Setup(r => r.Add(It.IsAny<LearningList>())).ReturnsAsync(learningList);
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(new List<Knowledge> { new Knowledge { Title = "", Id = parameters.KnowledgeIds[0], Visibility = KnowledgeVisibility.Private, CreatorId = Guid.NewGuid() } });

            // Act
            var result = await _createLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotAuthorized, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenExceptionIsThrown()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new CreateLearningListParams
            {
                Title = "Test Learning List",
                KnowledgeIds = new List<Guid> { Guid.NewGuid() }
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "", UserName = "" });
            _learningListRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<LearningList>>())).ThrowsAsync(new Exception());

            // Act
            var result = await _createLearningListUseCase.Execute(parameters);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UnknownError, result.Error);
        }
    }
}