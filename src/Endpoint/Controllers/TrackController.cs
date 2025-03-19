using Application.Interfaces;
using Application.UseCases.Tracks;
using AutoMapper;
using Domain.Enums;
using Endpoint.ApiRequests.Tracks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;

namespace Endpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private readonly ITrackService _trackService;
        private readonly IMapper _mapper;

        public TrackController(ITrackService trackService, IMapper mapper)
        {
            _trackService = trackService;
            _mapper = mapper;

            var config = new MapperConfiguration(cfg =>
                        {
                            cfg.CreateMap<GetTracksRequest, GetTracksParams>();
                            cfg.CreateMap<CreateTrackRequest, CreateTrackParams>();
                            cfg.CreateMap<UpdateTrackRequest, UpdateTrackParams>();
                            cfg.CreateMap<CreateDeleteTrackSubjectRequest, CreateDeleteTrackSubjectParams>();
                        });
            _mapper = config.CreateMapper();
        }

        [HttpGet(HttpRoute.GetDetailedTracks)]
        // [Authorize(Roles = nameof(Role.User))]
        public async Task<IActionResult> GetDetailedTracks()
        {
            var result = await _trackService.GetDetailedTracks();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.GetTracks)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> GetTracks([FromBody] GetTracksRequest request)
        {
            var Params = _mapper.Map<GetTracksParams>(request);

            var result = await _trackService.GetTracks(Params);

            return result.IsSuccess
                ? Ok(new
                {
                    data = result.Value,
                    paging = result.Paging
                })
                : BadRequest(result.Errors);
        }

        [HttpGet(HttpRoute.GetTrackById)]
        [Authorize]
        public async Task<IActionResult> GetTrackById(Guid id)
        {
            var result = await _trackService.GetTrackById(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.CreateTrack)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> CreateTrack([FromBody] CreateTrackRequest request)
        {
            var Params = _mapper.Map<CreateTrackParams>(request);

            var result = await _trackService.CreateTrack(Params);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.UpdateTrack)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> UpdateTrack([FromBody] UpdateTrackRequest request)
        {
            var Params = _mapper.Map<UpdateTrackParams>(request);

            var result = await _trackService.UpdateTrack(Params);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.Errors);
        }

        [HttpDelete(HttpRoute.DeleteTrack)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> DeleteTrack(Guid id)
        {
            var result = await _trackService.DeleteTrack(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.CreateDeleteTrackSubject)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> CreateDeleteTrackSubject([FromBody] CreateDeleteTrackSubjectRequest request)
        {
            var Params = _mapper.Map<CreateDeleteTrackSubjectParams>(request);

            var result = await _trackService.CreateDeleteTrackSubject(Params);

            return result.IsSuccess
                ? Ok(result.Value.ToString())
                : BadRequest(result.Errors);
        }
    }
}
