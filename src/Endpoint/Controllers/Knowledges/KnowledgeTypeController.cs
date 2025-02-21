using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Application.Interfaces.Knowledges;
using Application.UseCases.Knowledges.KnowledgeTypes;
using Endpoint.ApiRequests.Knowledges.KnowledgeTypes;
using Shared.Constants;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Endpoint.Controllers.Knowledges
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeTypeController : ControllerBase
    {
        private readonly IKnowledgeTypeService _knowledgeTypeService;
        private readonly IMapper _mapper;

        public KnowledgeTypeController(IKnowledgeTypeService knowledgeTypeService, IMapper mapper)
        {
            _knowledgeTypeService = knowledgeTypeService;
            _mapper = mapper;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CreateKnowledgeTypeRequest, CreateKnowledgeTypeParams>();
                cfg.CreateMap<UpdateKnowledgeTypeRequest, UpdateKnowledgeTypeParams>();
                cfg.CreateMap<AttachDetachKnowledgesRequest, AttachDetachKnowledgesParams>();
                cfg.CreateMap<GetKnowledgeTypesRequest, GetKnowledgeTypesParams>();

            });
            _mapper = config.CreateMapper();
        }

        [HttpPost(HttpRoute.GetKnowledgeTypes)]
        public async Task<IActionResult> GetKnowledgeTypes(GetKnowledgeTypesRequest request)
        {
            var Params = _mapper.Map<GetKnowledgeTypesParams>(request);
            var result = await _knowledgeTypeService.GetKnowledgeTypes(Params);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpGet(HttpRoute.GetKnowledgeTypeByGuid)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> GetKnowledgeTypeByGuid(Guid id)
        {
            var result = await _knowledgeTypeService.GetKnowledgeTypeByGuid(id);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.CreateKnowledgeType)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> CreateKnowledgeType([FromBody] CreateKnowledgeTypeRequest request)
        {
            var Params = _mapper.Map<CreateKnowledgeTypeParams>(request);
            var result = await _knowledgeTypeService.CreateKnowledgeType(Params);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.UpdateKnowledgeType)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> UpdateKnowledgeType([FromBody] UpdateKnowledgeTypeRequest request)
        {
            var Params = _mapper.Map<UpdateKnowledgeTypeParams>(request);
            var result = await _knowledgeTypeService.UpdateKnowledgeType(Params);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpDelete(HttpRoute.DeleteKnowledgeType)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> DeleteKnowledgeType(Guid id)
        {
            var result = await _knowledgeTypeService.DeleteKnowledgeType(id);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.AttachDetachKnowledges)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> AttachDetachKnowledges([FromBody] AttachDetachKnowledgesRequest request)
        {
            var Params = _mapper.Map<AttachDetachKnowledgesParams>(request);
            var result = await _knowledgeTypeService.AttachDetachKnowledges(Params);
            return result.IsSuccess ? Ok(result.Value.ToString()) : BadRequest(result.Errors);
        }
    }
}
