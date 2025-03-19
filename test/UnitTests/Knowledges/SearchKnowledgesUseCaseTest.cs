using Application.UseCases.Knowledges;
using AutoMapper;
using Domain.Entities.SingleIdEntities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Moq;
using Shared.Constants;
using Application.Mappings;
using Domain.Base;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Domain.Entities.SingleIdPivotEntities;

namespace UnitTests.Knowledges
{
    public class SearchKnowledgesUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<Knowledge>> _knowledgeRepositoryMock;
        private readonly Mock<IRepository<KnowledgeType>> _knowledgeTypeRepositoryMock;
        private readonly Mock<IRepository<KnowledgeTopic>> _knowledgeTopicRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly IMapper _mapper;
        private readonly SearchKnowledgesUseCase _searchKnowledgesUseCase;

        public SearchKnowledgesUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _knowledgeRepositoryMock = new Mock<IRepository<Knowledge>>();
            _knowledgeTypeRepositoryMock = new Mock<IRepository<KnowledgeType>>();
            _knowledgeTopicRepositoryMock = new Mock<IRepository<KnowledgeTopic>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _unitOfWorkMock.Setup(u => u.Repository<Knowledge>()).Returns(_knowledgeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<KnowledgeType>()).Returns(_knowledgeTypeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<KnowledgeTopic>()).Returns(_knowledgeTopicRepositoryMock.Object);

            _searchKnowledgesUseCase = new SearchKnowledgesUseCase(_unitOfWorkMock.Object, _mapper, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenNoKnowledgesFound()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = "NonExistentKnowledge",
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(Enumerable.Empty<Knowledge>());

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.NoKnowledgesFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenKnowledgesAreFound()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = "Introduction",
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            var knowledges = SeedData.GetKnowledges();

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledges);

            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync((Learning?)null);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
            Assert.Equal(knowledges.Length, result.Value.Count());

            var knowledgeDto = result.Value.First();
            Assert.Equal(knowledges[0].Id, knowledgeDto.Id);
            Assert.Equal(knowledges[0].Title, knowledgeDto.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenFilteredByKnowledgeType()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [SeedData.KnowledgeType1Id],
                KnowledgeTopicIds = [],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            var knowledgeTypes = SeedData.GetKnowledgeTypes();
            knowledgeTypes[1].Children = [knowledgeTypes[0]];

            var knowledgeTypeKnowledge = SeedData.GetKnowledgeTypeKnowledges()[1];
            knowledgeTypeKnowledge.KnowledgeType = knowledgeTypes[1];

            var knowledges = new List<Knowledge> { SeedData.GetKnowledges()[0] };
            knowledges[0].KnowledgeTypeKnowledges = [knowledgeTypeKnowledge];

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _knowledgeTypeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<KnowledgeType>>())).ReturnsAsync([knowledgeTypes[1]]);
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledges);

            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync((Learning?)null);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
            Assert.Equal(knowledges.Count, result.Value.Count());

            var knowledgeDto = result.Value.First();
            Assert.Equal(knowledges[0].Id, knowledgeDto.Id);
            Assert.Equal(knowledges[0].Title, knowledgeDto.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenFilteredByKnowledgeTopic()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [SeedData.KnowledgeTopic1Id],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            var knowledgeTopics = SeedData.GetKnowledgeTopics();
            knowledgeTopics[1].Children = [knowledgeTopics[0]];

            var knowledgeTopicKnowledge = SeedData.GetKnowledgeTopicKnowledges()[1];
            knowledgeTopicKnowledge.KnowledgeTopic = knowledgeTopics[1];

            var knowledges = new List<Knowledge> { SeedData.GetKnowledges()[0] };
            knowledges[0].KnowledgeTopicKnowledges = [knowledgeTopicKnowledge];

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _knowledgeTopicRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<KnowledgeTopic>>())).ReturnsAsync([knowledgeTopics[1]]);
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledges);

            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync((Learning?)null);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
            Assert.Equal(knowledges.Count, result.Value.Count());

            var knowledgeDto = result.Value.First();
            Assert.Equal(knowledges[0].Id, knowledgeDto.Id);
            Assert.Equal(knowledges[0].Title, knowledgeDto.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenFilteredByLevel()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [],
                Level = KnowledgeLevel.Beginner,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            var knowledges = SeedData.GetKnowledges().Where(k => k.Level == KnowledgeLevel.Beginner).ToList();

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledges);

            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync((Learning?)null);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
            Assert.Equal(knowledges.Count, result.Value.Count());

            var knowledgeDto = result.Value.First();
            Assert.Equal(knowledges[0].Id, knowledgeDto.Id);
            Assert.Equal(knowledges[0].Title, knowledgeDto.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenOrderedByTitle()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Title,
                Ascending = true
            };

            var knowledges = SeedData.GetKnowledges().OrderBy(k => k.Title).ToList();

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new Claim("sub", SeedData.GetUsers().First().Id.ToString()));
            _knowledgeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Knowledge>>())).ReturnsAsync(knowledges);

            var learningRepositoryMock = new Mock<IRepository<Learning>>();
            learningRepositoryMock.Setup(r => r.Find(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync((Learning?)null);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(learningRepositoryMock.Object);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
            Assert.Equal(knowledges.Count, result.Value.Count());

            var knowledgeDto = result.Value.First();
            Assert.Equal(knowledges[0].Id, knowledgeDto.Id);
            Assert.Equal(knowledges[0].Title, knowledgeDto.Title);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenNoKnowledgeTypesFound()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [Guid.NewGuid()],
                KnowledgeTopicIds = [],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            _knowledgeTypeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<KnowledgeType>>())).ReturnsAsync(Enumerable.Empty<KnowledgeType>());

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.NoKnowledgeTypesFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenSomeKnowledgeTypesNotFound()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [Guid.NewGuid(), Guid.NewGuid()],
                KnowledgeTopicIds = [],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            var knowledgeTypes = new List<KnowledgeType>
            {
                new KnowledgeType { Id = parameters.KnowledgeTypeIds[0], Name = "Type 1" }
            };

            _knowledgeTypeRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<KnowledgeType>>())).ReturnsAsync(knowledgeTypes);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.SomeKnowledgeTypesNotFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenNoKnowledgeTopicsFound()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [Guid.NewGuid()],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            _knowledgeTopicRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<KnowledgeTopic>>())).ReturnsAsync(Enumerable.Empty<KnowledgeTopic>());

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.NoKnowledgeTopicsFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenSomeKnowledgeTopicsNotFound()
        {
            var parameters = new SearchKnowledgesParams
            {
                SearchTerm = null,
                Page = 1,
                PageSize = 10,
                KnowledgeTypeIds = [],
                KnowledgeTopicIds = [Guid.NewGuid(), Guid.NewGuid()],
                Level = null,
                OrderBy = SearchKnowledgesParams.OrderByType.Date,
                Ascending = false
            };

            var knowledgeTopics = new List<KnowledgeTopic>
            {
                new KnowledgeTopic { Id = parameters.KnowledgeTopicIds[0], Title = "Topic 1" }
            };

            _knowledgeTopicRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<KnowledgeTopic>>())).ReturnsAsync(knowledgeTopics);

            var result = await _searchKnowledgesUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.SomeKnowledgeTopicsNotFound, result.Error);
        }
    }
}