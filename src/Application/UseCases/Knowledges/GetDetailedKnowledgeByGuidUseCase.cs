using Application.DTOs;
using Application.DTOs.SingleIdPivotEntities;
using Application.Interfaces;
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

namespace Application.UseCases.Knowledges
{
    public class GetDetailedKnowledgeByGuidUseCase : IUseCase<KnowledgeDto, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRedisCache _cache;


        public GetDetailedKnowledgeByGuidUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IRedisCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public async Task<Result<KnowledgeDto>> Execute(Guid id)
        {
            try
            {
                var cacheKey = $"{RedisCache.Keys.GetDetailedKnowledgeByGuid}_{id}";
                var knowledgeDto = await _cache.GetAsync<KnowledgeDto>(cacheKey);
                if (knowledgeDto == null)
                {
                    var knowledgeRepository = _unitOfWork.Repository<Knowledge>();
                    var knowledge = await knowledgeRepository.Find(
                        new BaseSpecification<Knowledge>(k => k.Id == id)
                        .AddInclude(query => query
                            .Include(k => k.KnowledgeTypeKnowledges)
                            .ThenInclude(kt => kt.KnowledgeType!)
                            .Include(k => k.KnowledgeTopicKnowledges)
                            .ThenInclude(kt => kt.KnowledgeTopic!)
                            .Include(k => k.Creator)
                            .Include(k => k.Materials)
                    ));

                    if (knowledge == null)
                        return Result<KnowledgeDto>.Fail(ErrorMessage.NoKnowledgeFoundWithGuid);

                    knowledgeDto = _mapper.Map<KnowledgeDto>(knowledge);
                    await _cache.SetAsync(cacheKey, knowledgeDto);
                }

                var userId = UserExtractor.GetUserId(_httpContextAccessor);
                var user = userId == null ? null : await _unitOfWork.Repository<User>().GetById(userId.Value);

                if (user != null && !user.IsAdmin && knowledgeDto.CreatorId != userId && knowledgeDto.Visibility == KnowledgeVisibility.Private.ToString())
                    return Result<KnowledgeDto>.Fail(ErrorMessage.NoKnowledgeFoundWithGuid);

                if (user != null && !user.IsAdmin)
                {
                    var userLearning = await _unitOfWork.Repository<Learning>().Find(
                        new BaseSpecification<Learning>(ul => ul.UserId == userId && ul.KnowledgeId == id)
                    );
                    knowledgeDto.CurrentUserLearning = userLearning == null ? null : _mapper.Map<LearningDto>(userLearning);
                }

                return Result<KnowledgeDto>.Done(knowledgeDto);
            }
            catch (Exception)
            {
                return Result<KnowledgeDto>.Fail(ErrorMessage.UnknownError);
            }
        }
    }
}
