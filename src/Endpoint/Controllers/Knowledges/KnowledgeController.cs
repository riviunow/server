using Application.Interfaces.Knowledges;
using Application.UseCases.Knowledges;
using AutoMapper;
using Domain.Enums;
using Endpoint.ApiRequests.Knowledges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Utils;

namespace Endpoint.Controllers.Knowledges
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeService _knowledgeService;
        private readonly IMapper _mapper;

        public KnowledgeController(IKnowledgeService knowledgeService, IMapper mapper)
        {
            _knowledgeService = knowledgeService;
            _mapper = mapper;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SearchKnowledgesRequest, SearchKnowledgesParams>()
                    .ForMember(dest => dest.Level, opt => opt.MapFrom(src => TypeConverter.StringToEnum<KnowledgeLevel>(src.Level)))
                    .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => TypeConverter.StringToEnum<SearchKnowledgesParams.OrderByType>(src.OrderBy)));
                cfg.CreateMap<GetCreatedKnowledgesRequest, GetCreatedKnowledgesParams>();
                cfg.CreateMap<GetKnowledgesRequest, GetKnowledgesParams>();
                cfg.CreateMap<CreateMaterialRequest, CreateMaterialParams>();
                cfg.CreateMap<CreateKnowledgeRequest, CreateKnowledgeParams>()
                    .ForMember(dest => dest.Level, opt => opt.MapFrom(src => TypeConverter.StringToEnum<KnowledgeLevel>(src.Level)));
                cfg.CreateMap<UpdateKnowledgeRequest, UpdateKnowledgeParams>()
                    .ForMember(dest => dest.Level, opt => opt.MapFrom(src => TypeConverter.StringToEnum<KnowledgeLevel>(src.Level)));
                cfg.CreateMap<GetKnowledgesToLearnRequest, GetKnowledgesToLearnParams>();
                cfg.CreateMap<MigrateKnowledgesRequest, MigrateKnowledgesParams>();
            });
            _mapper = config.CreateMapper();
        }

        [HttpPost(HttpRoute.SearchKnowledges)]
        public async Task<IActionResult> SearchKnowledges([FromBody] SearchKnowledgesRequest request)
        {
            var parameters = _mapper.Map<SearchKnowledgesParams>(request);
            var result = await _knowledgeService.SearchKnowledges(parameters);
            return result.IsSuccess ? Ok(new
            {
                data = result.Value,
                paging = result.Paging
            }) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.GetKnowledgesToLearn)]
        [Authorize(Roles = nameof(Role.User))]
        public async Task<IActionResult> GetKnowledgesToLearn([FromBody] GetKnowledgesToLearnRequest request)
        {
            var parameters = _mapper.Map<GetKnowledgesToLearnParams>(request);
            var result = await _knowledgeService.GetKnowledgesToLearn(parameters);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.GetCreatedKnowledges)]
        [Authorize(Roles = nameof(Role.User))]
        public async Task<IActionResult> GetCreatedKnowledges([FromBody] GetCreatedKnowledgesRequest request)
        {
            var parameters = _mapper.Map<GetCreatedKnowledgesParams>(request);
            var result = await _knowledgeService.GetCreatedKnowledges(parameters);
            return result.IsSuccess ? Ok(new
            {
                data = result.Value,
                paging = result.Paging
            }) : BadRequest(result.Errors);
        }

        [HttpGet(HttpRoute.GetDetailedKnowledgeByGuid)]
        // [Authorize]
        public async Task<IActionResult> GetDetailedKnowledgeByGuid(Guid id)
        {
            var result = await _knowledgeService.GetDetailedKnowledgeByGuid(id);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.GetKnowledges)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> GetKnowledges([FromBody] GetKnowledgesRequest request)
        {
            var parameters = _mapper.Map<GetKnowledgesParams>(request);
            var result = await _knowledgeService.GetKnowledges(parameters);
            return result.IsSuccess ? Ok(new
            {
                data = result.Value,
                paging = result.Paging
            }) : BadRequest(result.Errors);
        }


        [HttpPost(HttpRoute.CreateKnowledge)]
        [Authorize]
        public async Task<IActionResult> CreateKnowledge([FromForm] CreateKnowledgeRequest request)
        {
            var parameters = _mapper.Map<CreateKnowledgeParams>(request);
            var result = await _knowledgeService.CreateKnowledge(parameters);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.UpdateKnowledge)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> UpdateKnowledge([FromBody] UpdateKnowledgeRequest request)
        {
            var parameters = _mapper.Map<UpdateKnowledgeParams>(request);
            var result = await _knowledgeService.UpdateKnowledge(parameters);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpDelete(HttpRoute.DeleteKnowledge)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> DeleteKnowledge(Guid id)
        {
            var result = await _knowledgeService.DeleteKnowledge(id);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.MigrateKnowledges)]
        [Authorize(Roles = nameof(Role.User))]
        public async Task<IActionResult> MigrateKnowledges([FromBody] MigrateKnowledgesRequest request)
        {
            var parameters = _mapper.Map<MigrateKnowledgesParams>(request);
            var result = await _knowledgeService.MigrateKnowledges(parameters);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }
    }
}
