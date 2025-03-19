using System.Security.Claims;
using Application.Mappings;
using Application.UseCases.Knowledges.Learnings;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Entities.SingleIdPivotEntities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Constants;

namespace UnitTests.Knowledges.Learnings
{
    public class GetLearningsToReviewUseCaseTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<Knowledge>> _knowledgeRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IRepository<Learning>> _learningRepositoryMock;
        private readonly Mock<IRepository<GameKnowledgeSubscription>> _gameKnowledgeSubscriptionRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly IMapper _mapper;

        private readonly GetLearningsToReviewUseCase _GetLearningsToReviewUseCase;

        public GetLearningsToReviewUseCaseTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _knowledgeRepositoryMock = new Mock<IRepository<Knowledge>>();
            _learningRepositoryMock = new Mock<IRepository<Learning>>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            _gameKnowledgeSubscriptionRepositoryMock = new Mock<IRepository<GameKnowledgeSubscription>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _unitOfWorkMock.Setup(u => u.Repository<Knowledge>()).Returns(_knowledgeRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Learning>()).Returns(_learningRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<GameKnowledgeSubscription>()).Returns(_gameKnowledgeSubscriptionRepositoryMock.Object);

            _GetLearningsToReviewUseCase = new GetLearningsToReviewUseCase(_unitOfWorkMock.Object, _mapper, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenUserNotFound()
        {
            var parameters = new GetLearningsToReviewParams
            {
                KnowledgeIds = [Guid.NewGuid()]
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)).Returns((Claim?)null);

            var result = await _GetLearningsToReviewUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.UserNotFound, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenSomeKnowledgesHaveNotBeenLearned()
        {
            var userId = Guid.NewGuid();
            var parameters = new GetLearningsToReviewParams
            {
                KnowledgeIds = [Guid.NewGuid()]
            };

            var learnings = new List<Learning>
            {
                new Learning { UserId = userId, KnowledgeId = parameters.KnowledgeIds.First(), Knowledge = new Knowledge { Id = Guid.NewGuid(), Title = "Knowledge 1", Visibility = KnowledgeVisibility.Public } },
                new Learning { UserId = userId, KnowledgeId = Guid.NewGuid(), Knowledge = new Knowledge { Id = Guid.NewGuid(), Title = "Knowledge 1", Visibility = KnowledgeVisibility.Public } },
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new System.Security.Claims.Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "", UserName = "" });

            _learningRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync(learnings);

            var result = await _GetLearningsToReviewUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.SomeKnowledgesHaveNotBeenLearned, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenSomeKnowledgesAreNotReadyToReview()
        {
            var userId = Guid.NewGuid();
            var parameters = new GetLearningsToReviewParams
            {
                KnowledgeIds = [Guid.NewGuid()]
            };

            var learnings = new List<Learning>
            {
                new Learning {
                    UserId = userId,
                    KnowledgeId = parameters.KnowledgeIds.First(),
                    NextReviewDate = DateTime.UtcNow.AddDays(1),
                    Knowledge = new Knowledge { Id = Guid.NewGuid(), Title = "Knowledge 1", Visibility = KnowledgeVisibility.Public }},
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new System.Security.Claims.Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "", UserName = "" });

            _learningRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync(learnings);

            var result = await _GetLearningsToReviewUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.SomeKnowledgesAreNotReadyToReview, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnFail_WhenKnowledgeRequireAGameToReview()
        {
            var userId = Guid.NewGuid();
            var knowledgeId = Guid.NewGuid();
            var parameters = new GetLearningsToReviewParams
            {
                KnowledgeIds = [knowledgeId]
            };

            var learnings = new List<Learning>
            {
                new Learning {
                     UserId = userId,
                     KnowledgeId = knowledgeId,
                     Knowledge = new Knowledge
                    {
                        Id = knowledgeId,
                        Title = "Knowledge 1",
                        GameKnowledgeSubscriptions =
                        [],
                        Materials =
                        [
                            new Material { Type = MaterialType.Interpretation, Content = "Interpretation 1" },
                            new Material { Type = MaterialType.Interpretation, Content = "Interpretation 2" },
                            new Material { Type = MaterialType.TextMedium, Content = "Medium Text 3" }
                        ]
                    }
                }
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new System.Security.Claims.Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "", UserName = "" });

            _learningRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync(learnings);
            _gameKnowledgeSubscriptionRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<GameKnowledgeSubscription>>())).ReturnsAsync([]);

            var result = await _GetLearningsToReviewUseCase.Execute(parameters);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.RequireAGameToReview, result.Error);
        }

        [Fact]
        public async Task Execute_ShouldReturnSuccess_WhenKnowledgesAreFound()
        {
            var userId = Guid.NewGuid();
            var knowledgeId = Guid.NewGuid();
            var parameters = new GetLearningsToReviewParams
            {
                KnowledgeIds = [knowledgeId]
            };

            var learnings = new List<Learning>
            {
                new Learning {
                     UserId = userId,
                     KnowledgeId = knowledgeId,
                     Knowledge = new Knowledge
                {
                    Id = knowledgeId,
                    Title = "Knowledge 1",
                    GameKnowledgeSubscriptions =
                    [
                        new GameKnowledgeSubscription
                        {
                            GameId = Guid.NewGuid(),
                            KnowledgeId = knowledgeId,
                            GameOptions =
                            [
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Question, Value = "Option 1", IsCorrect = false, Group = 1 },
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Answer, Value = "Option 2", IsCorrect = true, Group = 1 },
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Question, Value = "Option 3", IsCorrect = false, Group = 2 },
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Answer, Value = "Option 4", IsCorrect = true, Group = 2 }
                            ]
                        },
                        new GameKnowledgeSubscription
                        {
                            GameId = Guid.NewGuid(),
                            KnowledgeId = knowledgeId,
                            GameOptions =
                            [
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Question, Value = "Option 1", IsCorrect = false, Group = 1 },
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Answer, Value = "Option 2", IsCorrect = true, Group = 1 },
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Question, Value = "Option 3", IsCorrect = false, Group = 2 },
                                new GameOption { Id = Guid.NewGuid(), Type = GameOptionType.Answer, Value = "Option 4", IsCorrect = true, Group = 2 }
                            ]
                        },
                    ],
                    Materials =
                    [
                        new Material { Type = MaterialType.Interpretation, Content = "Interpretation 1" },
                        new Material { Type = MaterialType.Interpretation, Content = "Interpretation 2" },
                        new Material { Type = MaterialType.TextMedium, Content = "Medium Text 3" }
                    ]
                }
                }
            };

            _httpContextAccessorMock.Setup(h => h.HttpContext!.User.FindFirst(It.IsAny<string>())).Returns(new System.Security.Claims.Claim("sub", userId.ToString()));
            _userRepositoryMock.Setup(r => r.GetById(userId)).ReturnsAsync(new User { Id = userId, Email = "Email", UserName = "UserName" });

            _learningRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<Learning>>())).ReturnsAsync(learnings);
            _gameKnowledgeSubscriptionRepositoryMock.Setup(r => r.FindMany(It.IsAny<BaseSpecification<GameKnowledgeSubscription>>())).ReturnsAsync(learnings.First().Knowledge!.GameKnowledgeSubscriptions);

            var result = await _GetLearningsToReviewUseCase.Execute(parameters);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.First());
            Assert.Equal(knowledgeId, result.Value.First().ToList().First().Knowledge!.Id);
            Assert.Equal("Knowledge 1", result.Value.First().ToList().First().Knowledge!.Title);
            Assert.Equal(3, result.Value.First().ToList().First().Knowledge!.Materials.Count);
            Assert.NotNull(result.Value.First().ToList().First().Knowledge!.GameToReview);
        }
    }
}