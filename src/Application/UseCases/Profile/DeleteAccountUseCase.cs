using Application.DTOs;
using AutoMapper;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Shared.Constants;
using Shared.Types;
using Shared.Utils;

namespace Application.UseCases.Profile;

public class DeleteAccountUseCase : IUseCase<UserDto, NoParam>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteAccountUseCase(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<UserDto>> Execute(NoParam parameters)
    {
        try
        {
            var userRepository = _unitOfWork.Repository<User>();
            var userId = UserExtractor.GetUserId(_httpContextAccessor);
            var user = userId == null ? null : await userRepository.GetById(userId.Value);
            if (user == null)
                return Result<UserDto>.Fail(ErrorMessage.UserNotFound);

            user = await userRepository.Delete(user);

            return Result<UserDto>.Done(_mapper.Map<UserDto>(user));
        }
        catch (Exception)
        {
            return Result<UserDto>.Fail(ErrorMessage.UnknownError);
        }
    }
}