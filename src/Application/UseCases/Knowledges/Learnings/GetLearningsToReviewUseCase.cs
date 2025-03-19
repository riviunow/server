using Application.DTOs.SingleIdPivotEntities;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Entities.SingleIdPivotEntities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Types;
using Shared.Utils;

namespace Application.UseCases.Knowledges.Learnings;


public class GetLearningsToReviewParams
{
    public List<Guid> KnowledgeIds { get; set; } = [];
}

public class GetLearningsToReviewUseCase : IUseCase<List<List<LearningDto>>, GetLearningsToReviewParams>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetLearningsToReviewUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<List<LearningDto>>>> Execute(GetLearningsToReviewParams parameters)
    {
        try
        {
            var userId = UserExtractor.GetUserId(_httpContextAccessor);
            var user = userId == null ? null : await _unitOfWork.Repository<User>().GetById(userId.Value);
            if (user == null)
                return Result<List<List<LearningDto>>>.Fail(ErrorMessage.UserNotFound);

            IEnumerable<Learning> learnings = await _unitOfWork.Repository<Learning>().FindMany(
                new BaseSpecification<Learning>(
                    l => l.UserId == userId && parameters.KnowledgeIds.Contains(l.KnowledgeId)
                ).AddInclude(query => query
                    .Include(l => l.LearningHistories)
                    .Include(l => l.Knowledge!)
                    .ThenInclude(k => k.Materials)
            ));
            learnings = learnings.Where(l => l.Knowledge!.Visibility == KnowledgeVisibility.Public || (l.Knowledge!.Visibility == KnowledgeVisibility.Private && l.Knowledge.CreatorId == userId));

            if (learnings.Count() != parameters.KnowledgeIds.Count)
                return Result<List<List<LearningDto>>>.Fail(ErrorMessage.SomeKnowledgesHaveNotBeenLearned);
            else if (learnings.Any(l => l.NextReviewDate > DateTime.UtcNow))
                return Result<List<List<LearningDto>>>.Fail(ErrorMessage.SomeKnowledgesAreNotReadyToReview);

            learnings = ArrangeLearningsByPriority(learnings);

            List<List<Learning>> LearningGroups = Randomer.GetRandomGroups(learnings.ToList());

            List<List<LearningDto>> LearningDataToReviewResponses = [];

            for (int i = 0; i < LearningGroups.Count; i++)
            {
                var _learningDtos = _mapper.Map<List<LearningDto>>(LearningGroups[i]);

                foreach (LearningDto? learning in _learningDtos)
                {
                    var gameKnowledgeSubscriptionsRepository = _unitOfWork.Repository<GameKnowledgeSubscription>();
                    IEnumerable<GameKnowledgeSubscription> gameKnowledgeSubscriptions = await gameKnowledgeSubscriptionsRepository.FindMany(
                        new BaseSpecification<GameKnowledgeSubscription>(gk =>
                            gk.KnowledgeId == learning.KnowledgeId
                            && !learning.LearningHistories
                                .Select(lh => lh.PlayedGameId).ToList()
                                .Contains(gk.GameId)
                        ).AddInclude(query => query
                            .Include(gk => gk.Game!)
                            .Include(gk => gk.GameOptions)
                        )
                    );

                    if (gameKnowledgeSubscriptions.Count() < 1)
                    {
                        gameKnowledgeSubscriptions = await gameKnowledgeSubscriptionsRepository.FindMany(
                            new BaseSpecification<GameKnowledgeSubscription>(gk => gk.KnowledgeId == learning.KnowledgeId).AddInclude(query => query
                            .Include(gk => gk.Game!)
                            .Include(gk => gk.GameOptions)
                        ));
                        if (gameKnowledgeSubscriptions.Count() < 1)
                            return Result<List<List<LearningDto>>>.Fail(ErrorMessage.RequireAGameToReview);
                    }

                    if (gameKnowledgeSubscriptions.Count() > 1)
                        gameKnowledgeSubscriptions = Randomer.GetRandomElementAsList(gameKnowledgeSubscriptions.ToList());

                    learning.Knowledge!.GameToReview = _mapper.Map<GameKnowledgeSubscriptionDto>(gameKnowledgeSubscriptions.First()).DistinctGroupedGameOptions();
                }

                LearningDataToReviewResponses.Add(_learningDtos);
            }

            return Result<List<List<LearningDto>>>.Done(LearningDataToReviewResponses);
        }
        catch (Exception)
        {
            return Result<List<List<LearningDto>>>.Fail(ErrorMessage.UnknownError);
        }
    }

    /// <summary>
    /// Arrange learnings by learning level, memorization status and next review date
    /// </summary>
    /// <param name="learnings">list of learnings</param>
    /// <returns>return arranged learnings</returns>
    private static IEnumerable<Learning> ArrangeLearningsByPriority(IEnumerable<Learning> learnings)
    {
        var levels = new List<IEnumerable<Learning>>();
        for (int i = 0; i <= 5; i++)
        {
            levels.Add(learnings.Where(l => l.LatestLearningHistory.LearningLevel == (LearningLevel)i).ToList());
        }

        var arrangedLevels = new List<Learning>();
        foreach (var level in levels)
        {
            if (level.Count() > 1)
            {
                var memorized = level.Where(l => l.LatestLearningHistory.IsMemorized).OrderBy(l => l.NextReviewDate);
                var notMemorized = level.Where(l => !l.LatestLearningHistory.IsMemorized).OrderBy(l => l.NextReviewDate);

                arrangedLevels.AddRange(memorized.Concat(notMemorized));
            }
            else
            {
                arrangedLevels.AddRange(level);
            }
        }

        return arrangedLevels;
    }
}
