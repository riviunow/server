using Application.UseCases.Knowledges;
using AutoMapper;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Moq;
using Shared.Constants;
using Application.Mappings;
using Domain.Base;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Domain.Enums;
using Application.Interfaces;
using Domain.Entities.PivotEntities;
using Domain.Entities.SingleIdPivotEntities;

namespace UnitTests.Knowledges
{
    public class GetDetailedKnowledgeByGuidUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<Knowledge>> _knowledgeRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IRedisCache> _cacheMock;
        private readonly IMapper _mapper;
        private readonly GetDetailedKnowledgeByGuidUseCase _getDetailedKnowledgeByGuidUseCase;

        public GetDetailedKnowledgeByGuidUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _knowledgeRepositoryMock = new Mock<IRepository<Knowledge>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _cacheMock = new Mock<IRedisCache>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _unitOfWorkMock.Setup(u => u.Repository<Knowledge>()).Returns(_knowledgeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_userRepositoryMock.Object);

            _getDetailedKnowledgeByGuidUseCase = new GetDetailedKnowledgeByGuidUseCase(_unitOfWorkMock.Object, _mapper, _httpContextAccessorMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenKnowledgeNotFound()
        {
            var knowledgeId = Guid.NewGuid();

            _knowledgeRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync((Knowledge?)null);

            var result = await _getDetailedKnowledgeByGuidUseCase.Execute(knowledgeId);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.NoKnowledgeFoundWithGuid, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenKnowledgeVisibilityIsPrivate()
        {
            var knowledge = new Knowledge
            {
                Id = Guid.NewGuid(),
                Title = "Test Knowledge",
                KnowledgeTypeKnowledges = new List<KnowledgeTypeKnowledge>(),
                KnowledgeTopicKnowledges = new List<KnowledgeTopicKnowledge>(),
                SubjectKnowledges = new List<SubjectKnowledge>(),
                Materials = new List<Material>(),
                Visibility = KnowledgeVisibility.Private
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(SeedData.GetUsers().First().Id)).ReturnsAsync(SeedData.GetUsers().First());
            _knowledgeRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledge);

            var result = await _getDetailedKnowledgeByGuidUseCase.Execute(knowledge.Id);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.NoKnowledgeFoundWithGuid, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenKnowledgeIsFound()
        {
            var knowledge = new Knowledge
            {
                Id = Guid.NewGuid(),
                Title = "Test Knowledge",
                Visibility = KnowledgeVisibility.Public,
                KnowledgeTypeKnowledges = new List<KnowledgeTypeKnowledge>(),
                KnowledgeTopicKnowledges = new List<KnowledgeTopicKnowledge>(),
                SubjectKnowledges = new List<SubjectKnowledge>(),
                Creator = SeedData.GetUsers().First(),
                Materials = new List<Material>()
            };

            _knowledgeRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledge);
            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(SeedData.GetUsers().First().Id)).ReturnsAsync(SeedData.GetUsers().First());
            var learning = new Learning
            {
                UserId = SeedData.GetUsers().First().Id,
                KnowledgeId = knowledge.Id
            };
            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync(learning);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);

            var result = await _getDetailedKnowledgeByGuidUseCase.Execute(knowledge.Id);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(knowledge.Id, result.Value.Id);
            Assert.Equal(knowledge.Title, result.Value.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WithRelatedEntities()
        {
            var knowledge = new Knowledge
            {
                Id = Guid.NewGuid(),
                Visibility = KnowledgeVisibility.Public,
                Title = "Test Knowledge",
                KnowledgeTypeKnowledges = SeedData.GetKnowledgeTypeKnowledges(),
                KnowledgeTopicKnowledges = SeedData.GetKnowledgeTopicKnowledges(),
                SubjectKnowledges = SeedData.GetSubjectKnowledges(),
                Creator = SeedData.GetUsers().First(),
                Materials = SeedData.GetMaterials()
            };

            _knowledgeRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledge);
            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(SeedData.GetUsers().First().Id)).ReturnsAsync(SeedData.GetUsers().First());
            var learning = new Learning
            {
                UserId = SeedData.GetUsers().First().Id,
                KnowledgeId = knowledge.Id
            };
            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync(learning);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);
            var result = await _getDetailedKnowledgeByGuidUseCase.Execute(knowledge.Id);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(knowledge.Id, result.Value.Id);
            Assert.Equal(knowledge.Title, result.Value.Title);
            Assert.NotEmpty(result.Value.KnowledgeTypeKnowledges);
            Assert.NotEmpty(result.Value.KnowledgeTopicKnowledges);
            Assert.NotEmpty(result.Value.SubjectKnowledges);
            Assert.NotNull(result.Value.Creator);
            Assert.NotEmpty(result.Value.Materials);
        }
    }
}
