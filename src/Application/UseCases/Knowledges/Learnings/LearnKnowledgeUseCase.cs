using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Entities.SingleIdPivotEntities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Types;
using Shared.Utils;
using Domain.Enums;
using Domain.Entities.PivotEntities;
using Application.DTOs.SingleIdPivotEntities;

namespace Application.UseCases.Knowledges.Learnings;

public class LearnKnowledgeParams
{
    public Guid KnowledgeId { get; set; }
    public Guid QuestionIdOne { get; set; }
    public required string AnswerOne { get; set; }
    public Guid QuestionIdTwo { get; set; }
    public required string AnswerTwo { get; set; }
    public required string Interpretation { get; set; }
    public required string WordMatchAnswer { get; set; }
}

public class LearnKnowledgeUseCase : IUseCase<List<LearningDto>, List<LearnKnowledgeParams>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LearnKnowledgeUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<Result<List<LearningDto>>> Execute(List<LearnKnowledgeParams> parameters)
    {
        try
        {
            var knowledgeRepository = _unitOfWork.Repository<Knowledge>();
            var gameOptionRepository = _unitOfWork.Repository<GameOption>();
            var learningRepository = _unitOfWork.Repository<Learning>();
            var learningHistoryRepository = _unitOfWork.Repository<LearningHistory>();
            var learningListKnowledgeRepository = _unitOfWork.Repository<LearningListKnowledge>();

            var userId = UserExtractor.GetUserId(_httpContextAccessor);

            var knowledgesCount = await knowledgeRepository.Count(
                new BaseSpecification<Knowledge>(k => parameters.Select(p => p.KnowledgeId).Contains(k.Id) && (k.Visibility == KnowledgeVisibility.Public || (k.Visibility == KnowledgeVisibility.Private && k.CreatorId == userId)))
            );

            if (knowledgesCount != parameters.Count)
                return Result<List<LearningDto>>.Fail(ErrorMessage.SomeKnowledgesNotFound);

            var user = userId == null ? null : await _unitOfWork.Repository<User>().GetById(userId.Value);
            if (user == null)
                return Result<List<LearningDto>>.Fail(ErrorMessage.UserNotFound);

            var learningsCount = await learningRepository.Count(
                new BaseSpecification<Learning>(l => l.UserId == userId && parameters.Select(gks => gks.KnowledgeId).Contains(l.KnowledgeId))
            );
            if (learningsCount != 0)
                return Result<List<LearningDto>>.Fail(ErrorMessage.SomeKnowledgesAlreadyLearned);

            List<LearningDto> learningsResponse = [];

            foreach (var param in parameters)
            {
                var score = 0;

                var isGuid1 = Guid.TryParse(param.AnswerOne, out var answerGuid1);
                var QuestionOne = await gameOptionRepository.Find(
                    new BaseSpecification<GameOption>(go =>
                        go.Id == param.QuestionIdOne)
                    .ApplyTracking(true)
                    .AddInclude(query => query
                        .Include(go => go.GameKnowledgeSubscription!)
                        .ThenInclude(gks => gks.GameOptions!))
                );
                var userAnswerOne = isGuid1
                    ? QuestionOne!.GameKnowledgeSubscription!.GameOptions.FirstOrDefault(go => go.Group == QuestionOne.Group && go.Type == GameOptionType.Answer && go.Id == answerGuid1)
                    : QuestionOne!.GameKnowledgeSubscription!.GameOptions.FirstOrDefault(go => go.Group == QuestionOne.Group && go.Type == GameOptionType.Answer && go.Value.ToLower().Equals(param.AnswerOne.ToLower()));

                var isGuid2 = Guid.TryParse(param.AnswerTwo, out var answerGuid2);
                var QuestionTwo = await gameOptionRepository.Find(
                    new BaseSpecification<GameOption>(go =>
                        go.Id == param.QuestionIdTwo)
                    .ApplyTracking(true)
                    .AddInclude(query => query
                        .Include(go => go.GameKnowledgeSubscription!)
                        .ThenInclude(gks => gks.GameOptions!))
                );
                var userAnswerTwo = isGuid2
                    ? QuestionTwo!.GameKnowledgeSubscription!.GameOptions.FirstOrDefault(go => go.Group == QuestionTwo.Group && go.Type == GameOptionType.Answer && go.Id == answerGuid2)
                    : QuestionTwo!.GameKnowledgeSubscription!.GameOptions.FirstOrDefault(go => go.Group == QuestionTwo.Group && go.Type == GameOptionType.Answer && go.Value.ToLower().Equals(param.AnswerTwo.ToLower()));


                if ((isGuid1 && isGuid2 && param.AnswerOne == param.AnswerTwo)
                || QuestionOne == null
                || QuestionTwo == null
                || QuestionOne.GameKnowledgeSubscription!.KnowledgeId != param.KnowledgeId
                || QuestionTwo.GameKnowledgeSubscription!.KnowledgeId != param.KnowledgeId
                )
                {
                    return Result<List<LearningDto>>.Fail(ErrorMessage.InvalidData);
                }

                if (userAnswerOne != null && userAnswerOne.IsCorrect == true)
                    score += 35;
                if (userAnswerTwo != null && userAnswerTwo.IsCorrect == true)
                    score += 35;
                if (param.WordMatchAnswer == param.Interpretation)
                    score += 30;

                var newLearning = new Learning
                {
                    UserId = user.Id,
                    KnowledgeId = param.KnowledgeId,
                    NextReviewDate = DateTime.UtcNow + (score != 0 ? NeededReviewTime.Level0 : NeededReviewTime.NotMemorized),
                };
                newLearning = await learningRepository.Add(newLearning);
                await learningHistoryRepository.Add(
                    new LearningHistory
                    {
                        LearningId = newLearning.Id,
                        LearningLevel = LearningLevel.LevelZero,
                        IsMemorized = score >= 35,
                        PlayedGameId = QuestionOne.GameKnowledgeSubscription!.GameId,
                        Score = score,
                    });
                await learningHistoryRepository.Add(
                    new LearningHistory
                    {
                        LearningId = newLearning.Id,
                        LearningLevel = LearningLevel.LevelZero,
                        IsMemorized = score >= 35,
                        PlayedGameId = QuestionTwo.GameKnowledgeSubscription!.GameId,
                        Score = score,
                    });

                newLearning = await learningRepository.Find(
                    new BaseSpecification<Learning>(l => l.Id == newLearning.Id)
                    .AddInclude(query => query
                        .Include(l => l.Knowledge!)
                        .Include(l => l.LearningHistories))
                );

                var learningListKnowledgeCount = await learningListKnowledgeRepository.Count(
                    new BaseSpecification<LearningListKnowledge>(llk => llk.LearningList!.LearnerId == userId && llk.KnowledgeId == param.KnowledgeId)
                    .AddInclude(query => query.Include(llk => llk.LearningList!))
                );
                var learningDto = _mapper.Map<LearningDto>(newLearning);
                learningDto.LearningListCount = learningListKnowledgeCount;

                learningsResponse.Add(learningDto);
            }

            return Result<List<LearningDto>>.Done(learningsResponse);
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackChangesAsync();
            return Result<List<LearningDto>>.Fail(ErrorMessage.UnknownError);
        }
    }
}
