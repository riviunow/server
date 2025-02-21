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

public class ReviewLearningParams
{
    public Guid KnowledgeId;
    public Guid QuestionId;
    public required string Answer;
    public required string Interpretation;
    public required string WordMatchAnswer;
}

public class ReviewLearningUseCase : IUseCase<List<LearningDto>, List<ReviewLearningParams>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReviewLearningUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<LearningDto>>> Execute(List<ReviewLearningParams> parameters)
    {
        try
        {
            var gameOptionRepository = _unitOfWork.Repository<GameOption>();
            var learningRepository = _unitOfWork.Repository<Learning>();
            var learningHistoryRepository = _unitOfWork.Repository<LearningHistory>();

            var userId = UserExtractor.GetUserId(_httpContextAccessor);
            var user = userId == null ? null : await _unitOfWork.Repository<User>().GetById(userId.Value);
            if (user == null)
                return Result<List<LearningDto>>.Fail(ErrorMessage.UserNotFound);

            List<LearningDto> learningsResponse = [];

            foreach (var param in parameters)
            {
                var score = 0;

                var learning = await learningRepository.Find(
                    new BaseSpecification<Learning>(l =>
                    l.UserId == userId
                    && param.KnowledgeId == l.KnowledgeId
                    && (l.Knowledge!.Visibility == KnowledgeVisibility.Public
                            || (l.Knowledge.Visibility == KnowledgeVisibility.Private && l.Knowledge.CreatorId == userId)))
                    .AddInclude(query => query.Include(l => l.LearningHistories).Include(l => l.Knowledge!)));
                if (learning == null)
                    return Result<List<LearningDto>>.Fail(ErrorMessage.LearningNotFound);

                else if (learning.NextReviewDate > DateTime.UtcNow)
                    return Result<List<LearningDto>>.Fail(ErrorMessage.KnowledgeNotReadyToReview);

                else if (learning.LearningHistories.Count == 0)
                    return Result<List<LearningDto>>.Fail(ErrorMessage.RequireLearningBeforeReview);


                var isGuid = Guid.TryParse(param.Answer, out var answerGuid);
                var Question = await gameOptionRepository.Find(
                    new BaseSpecification<GameOption>(go =>
                        go.Id == param.QuestionId)
                    .ApplyTracking(true)
                    .AddInclude(query => query
                        .Include(go => go.GameKnowledgeSubscription!)
                        .ThenInclude(gks => gks.GameOptions!))
                );
                var userAnswer = isGuid
                    ? Question!.GameKnowledgeSubscription!.GameOptions.FirstOrDefault(go => go.Group == Question.Group && go.Type == GameOptionType.Answer && go.Id == answerGuid)
                    : Question!.GameKnowledgeSubscription!.GameOptions.FirstOrDefault(go => go.Group == Question.Group && go.Type == GameOptionType.Answer && go.Value.ToLower().Equals(param.Answer.ToLower()));


                if (Question == null
                || Question.GameKnowledgeSubscription!.KnowledgeId != param.KnowledgeId)
                    return Result<List<LearningDto>>.Fail(ErrorMessage.InvalidData);

                if (userAnswer != null && userAnswer.IsCorrect == true)
                    score += 75;

                if (param.WordMatchAnswer == param.Interpretation)
                    score += 25;

                var LatestLearningHistory = learning.LatestLearningHistory;
                var IsMemorized = score >= 35;

                learning.NextReviewDate = DateTime.UtcNow + (IsMemorized ? GetNextReviewTime(LatestLearningHistory!.LearningLevel) : NeededReviewTime.NotMemorized);
                await learningRepository.Update(learning);

                var newLearningHistory = new LearningHistory
                {
                    LearningId = learning.Id,
                    LearningLevel = IsMemorized ? GetNextLevel(LatestLearningHistory!.LearningLevel) : LatestLearningHistory!.LearningLevel,
                    IsMemorized = IsMemorized,
                    PlayedGameId = Question.GameKnowledgeSubscription!.GameId,
                    Score = score,
                };
                await learningHistoryRepository.Add(newLearningHistory);

                learning.LearningHistories.Add(newLearningHistory);
                learningsResponse.Add(_mapper.Map<LearningDto>(learning));
            }

            return Result<List<LearningDto>>.Done(learningsResponse);
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackChangesAsync();
            return Result<List<LearningDto>>.Fail(ErrorMessage.UnknownError);
        }
    }

    public static TimeSpan GetNextReviewTime(LearningLevel level)
    {
        return level switch
        {
            LearningLevel.LevelZero => NeededReviewTime.Level1,
            LearningLevel.LevelOne => NeededReviewTime.Level2,
            LearningLevel.LevelTwo => NeededReviewTime.Level3,
            LearningLevel.LevelThree => NeededReviewTime.Level4,
            LearningLevel.LevelFour => NeededReviewTime.Level5,
            LearningLevel.LevelFive => NeededReviewTime.Level5,
            _ => NeededReviewTime.Level0
        };
    }
    public static LearningLevel GetNextLevel(LearningLevel level)
    {
        return level switch
        {
            LearningLevel.LevelZero => LearningLevel.LevelOne,
            LearningLevel.LevelOne => LearningLevel.LevelTwo,
            LearningLevel.LevelTwo => LearningLevel.LevelThree,
            LearningLevel.LevelThree => LearningLevel.LevelFour,
            LearningLevel.LevelFour => LearningLevel.LevelFive,
            LearningLevel.LevelFive => LearningLevel.LevelFive,
            _ => LearningLevel.LevelZero
        };
    }
}
