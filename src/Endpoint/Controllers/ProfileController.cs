using Application.Interfaces;
using Application.UseCases.Profile;
using AutoMapper;
using Domain.Enums;
using Endpoint.ApiRequests.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;

namespace Endpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IMapper _mapper;

        public ProfileController(IProfileService profileService, IMapper mapper)
        {
            _profileService = profileService;
            _mapper = mapper;
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UpdateProfileRequest, UpdateProfileParams>();
            });
            _mapper = config.CreateMapper();
        }

        [HttpGet(HttpRoute.GetProfile)]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _profileService.GetProfile();
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors);
        }

        [HttpPost(HttpRoute.UpdateProfile)]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            var Params = _mapper.Map<UpdateProfileParams>(request);
            var result = await _profileService.UpdateProfile(Params);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors);
        }

        [HttpDelete(HttpRoute.DeleteAccount)]
        [Authorize(Roles = nameof(Role.User))]
        public async Task<IActionResult> DeleteAccount()
        {
            var DeleteAccountResult = await _profileService.DeleteAccount();
            return DeleteAccountResult.IsSuccess ? Ok(DeleteAccountResult.Value) : BadRequest(DeleteAccountResult.Errors);
        }
    }
}