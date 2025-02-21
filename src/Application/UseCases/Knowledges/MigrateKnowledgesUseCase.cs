using Application.DTOs.SingleIdPivotEntities;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Entities.SingleIdPivotEntities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Shared.Constants;
using Shared.Types;
using Shared.Utils;

namespace Application.UseCases.Knowledges
{
    public class MigrateKnowledgesParams
    {
        public List<Guid> KnowledgeIds { get; set; } = [];
    }

    public class MigrateKnowledgesUseCase : IUseCase<IEnumerable<LearningDto>, MigrateKnowledgesParams>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MigrateKnowledgesUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<IEnumerable<LearningDto>>> Execute(MigrateKnowledgesParams parameters)
        {
            try
            {
                var userId = UserExtractor.GetUserId(_httpContextAccessor);
                var user = userId == null ? null : await _unitOfWork.Repository<User>().GetById(userId.Value);
                if (user == null)
                    return Result<IEnumerable<LearningDto>>.Fail(ErrorMessage.UserNotFound);

                var knowledges = await _unitOfWork.Repository<Knowledge>().FindMany(
                    new BaseSpecification<Knowledge>(k => parameters.KnowledgeIds.Contains(k.Id) && k.Visibility == KnowledgeVisibility.Public)
                );
                if (knowledges.Count() != parameters.KnowledgeIds.Count)
                    return Result<IEnumerable<LearningDto>>.Fail(ErrorMessage.SomeKnowledgesNotFound);

                var learningRepository = _unitOfWork.Repository<Learning>();
                var learnings = new List<Learning>();
                foreach (var knowledge in knowledges)
                {
                    var existingLearning = await learningRepository.FindMany(
                        new BaseSpecification<Learning>(l => l.UserId == user.Id && l.KnowledgeId == knowledge.Id)
                    );
                    if (existingLearning.Any())
                        continue;

                    var learning = new Learning
                    {
                        KnowledgeId = knowledge.Id,
                        UserId = user.Id,
                    };
                    await learningRepository.Add(learning);

                    learning.Knowledge = knowledge;
                    learnings.Add(learning);

                    var learningHistory = new LearningHistory
                    {
                        LearningId = learning.Id,
                        LearningLevel = LearningLevel.LevelFour,
                        IsMemorized = true,
                        PlayedGameId = null,
                        Score = 100,
                    };
                    await _unitOfWork.Repository<LearningHistory>().Add(learningHistory);
                }
                var learningDtos = _mapper.Map<IEnumerable<LearningDto>>(learnings);
                return Result<IEnumerable<LearningDto>>.Done(learningDtos);
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}