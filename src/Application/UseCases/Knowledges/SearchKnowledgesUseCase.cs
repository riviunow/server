using Application.DTOs;
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

namespace Application.UseCases.Knowledges
{
    public class SearchKnowledgesParams
    {
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<Guid> KnowledgeTypeIds { get; set; } = [];
        public List<Guid> KnowledgeTopicIds { get; set; } = [];
        public KnowledgeLevel? Level { get; set; }
        public OrderByType OrderBy { get; set; } = OrderByType.Date;
        public bool Ascending { get; set; } = false;

        public enum OrderByType
        {
            Date,
            Title,
        }
    }

    public class SearchKnowledgesUseCase : IUseCase<IEnumerable<KnowledgeDto>, SearchKnowledgesParams>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SearchKnowledgesUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<IEnumerable<KnowledgeDto>>> Execute(SearchKnowledgesParams parameters)
        {
            try
            {
                if (parameters.KnowledgeTypeIds.Count != 0)
                {
                    var knowledgeTypeRepository = _unitOfWork.Repository<KnowledgeType>();
                    var knowledgeTypes = await knowledgeTypeRepository.FindMany(new BaseSpecification<KnowledgeType>(kt => parameters.KnowledgeTypeIds.Contains(kt.Id)));
                    if (!knowledgeTypes.Any())
                    {
                        return Result<IEnumerable<KnowledgeDto>>.Fail(ErrorMessage.NoKnowledgeTypesFound);
                    }
                    else if (knowledgeTypes.Count() != parameters.KnowledgeTypeIds.Count)
                    {
                        return Result<IEnumerable<KnowledgeDto>>.Fail(ErrorMessage.SomeKnowledgeTypesNotFound);
                    }
                }

                if (parameters.KnowledgeTopicIds.Count != 0)
                {
                    var knowledgeTopicRepository = _unitOfWork.Repository<KnowledgeTopic>();
                    var knowledgeTopics = await knowledgeTopicRepository.FindMany(new BaseSpecification<KnowledgeTopic>(kt => parameters.KnowledgeTopicIds.Contains(kt.Id)));
                    if (!knowledgeTopics.Any())
                    {
                        return Result<IEnumerable<KnowledgeDto>>.Fail(ErrorMessage.NoKnowledgeTopicsFound);
                    }
                    else if (knowledgeTopics.Count() != parameters.KnowledgeTopicIds.Count)
                    {
                        return Result<IEnumerable<KnowledgeDto>>.Fail(ErrorMessage.SomeKnowledgeTopicsNotFound);
                    }
                }

                var knowledgeRepository = _unitOfWork.Repository<Knowledge>();

                var userId = UserExtractor.GetUserId(_httpContextAccessor);

                ISpecification<Knowledge> specification = new BaseSpecification<Knowledge>(k =>
                    (k.Visibility == KnowledgeVisibility.Public || (userId != null && k.Visibility == KnowledgeVisibility.Private && k.CreatorId == userId))
                    && (string.IsNullOrEmpty(parameters.SearchTerm)
                        || k.Title.Contains(parameters.SearchTerm))
                    && (parameters.KnowledgeTypeIds.Count == 0
                        || k.KnowledgeTypeKnowledges.Any(ktk =>
                            parameters.KnowledgeTypeIds.Contains(ktk.KnowledgeTypeId)
                                || ktk.KnowledgeType!.Children.Any(c => parameters.KnowledgeTypeIds.Contains(c.Id))))
                    && (parameters.KnowledgeTopicIds.Count == 0
                        || k.KnowledgeTopicKnowledges.Any(ktk =>
                            parameters.KnowledgeTopicIds.Contains(ktk.KnowledgeTopicId)
                                || ktk.KnowledgeTopic!.Children.Any(c => parameters.KnowledgeTopicIds.Contains(c.Id))))
                    && (!parameters.Level.HasValue
                        || k.Level == parameters.Level))
                    .AddInclude(query => query
                        .Include(k => k.KnowledgeTypeKnowledges)
                        .ThenInclude(ktk => ktk.KnowledgeType!)
                        .ThenInclude(kt => kt.Children)
                        .Include(k => k.KnowledgeTopicKnowledges)
                        .ThenInclude(ktk => ktk.KnowledgeTopic!)
                        .ThenInclude(kt => kt.Children));
                if (!string.IsNullOrEmpty(parameters.SearchTerm))
                    specification.AddOrderByDescending(k => k.Title.Equals(parameters.SearchTerm));
                else
                    switch (parameters.OrderBy)
                    {
                        case SearchKnowledgesParams.OrderByType.Date:
                            if (parameters.Ascending)
                                specification.AddOrderBy(k => k.CreatedAt);
                            else
                                specification.AddOrderByDescending(k => k.CreatedAt);
                            break;
                        case SearchKnowledgesParams.OrderByType.Title:
                            if (parameters.Ascending)
                                specification.AddOrderBy(k => k.Title);
                            else
                                specification.AddOrderByDescending(k => k.Title);
                            break;
                    }

                var knowledgeCount = await knowledgeRepository.Count(specification);
                var knowledges = await knowledgeRepository.FindMany(specification.ApplyPaging(parameters.Page, parameters.PageSize));

                if (!knowledges.Any())
                    return Result<IEnumerable<KnowledgeDto>>.Fail(ErrorMessage.NoKnowledgesFound);

                var knowledgeDtos = _mapper.Map<IEnumerable<KnowledgeDto>>(knowledges);
                if (userId != null)
                {
                    foreach (var knowledgeDto in knowledgeDtos.ToList())
                    {
                        var learning = await _unitOfWork.Repository<Learning>().Find(
                            new BaseSpecification<Learning>(l => l.KnowledgeId == knowledgeDto.Id && l.UserId == userId));
                        knowledgeDto.CurrentUserLearning = learning == null ? null : _mapper.Map<LearningDto>(learning);
                    }
                }

                return Result<IEnumerable<KnowledgeDto>>.Done(knowledgeDtos, new Paging(parameters.Page, parameters.PageSize, knowledgeCount));
            }
            catch (Exception)
            {
                return Result<IEnumerable<KnowledgeDto>>.Fail(ErrorMessage.UnknownError);
            }
        }
    }
}
