using Application.DTOs;
using Application.UseCases.JWT;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Constants;
using Shared.Types;

namespace Application.UseCases.Auth;

public class ConfirmRegistrationEmailParams
{
    public required string Email { get; set; }
    public required string ConfirmationCode { get; set; }
}
public class ConfirmRegistrationEmailUseCase : IUseCase<(UserDto, JWTPairResponse), ConfirmRegistrationEmailParams>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly GenerateTokenPairUseCase _generateTokenPairUseCase;
    private readonly IConfiguration _configuration;

    public ConfirmRegistrationEmailUseCase(IUnitOfWork unitOfWork, IMapper mapper, GenerateTokenPairUseCase generateTokenPairUseCase, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _generateTokenPairUseCase = generateTokenPairUseCase;
        _configuration = configuration;
    }

    public async Task<Result<(UserDto, JWTPairResponse)>> Execute(ConfirmRegistrationEmailParams parameters)
    {
        try
        {
            var userRepository = _unitOfWork.Repository<User>();
            var user = await userRepository.Find(
                new BaseSpecification<User>(u => u.Email == parameters.Email)
                .AddInclude(query => query.Include(u => u.Authentication!)));

            if (user == null || user.Authentication == null)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.UserNotFoundWithEmail);
            else if (user.Authentication.ConfirmationCode == null || user.Authentication.IsEmailConfirmed || user.Authentication.ConfirmationCodeExpiryTime == null)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.EmailAlreadyConfirmed);
            else if (user.Authentication.ConfirmationCode != parameters.ConfirmationCode)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.InvalidConfirmationCode);
            else if (user.Authentication.ConfirmationCodeExpiryTime < DateTime.UtcNow)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.ConfirmationCodeExpired);

            var tokenPairResult = await _generateTokenPairUseCase.Execute(user);
            if (!tokenPairResult.IsSuccess)
                return Result<(UserDto, JWTPairResponse)>.Fail(tokenPairResult.Error);
            var tokenPair = tokenPairResult.Value;

            var authentication = user.Authentication;
            authentication.User = null;
            authentication.IsEmailConfirmed = true;
            authentication.ConfirmationCode = null;
            authentication.ConfirmationCodeExpiryTime = null;
            authentication.RefreshToken = tokenPair.RefreshToken;
            authentication.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]));
            await _unitOfWork.Repository<Authentication>().Update(authentication);
            user.Authentication = null;

            return Result<(UserDto, JWTPairResponse)>.Done((_mapper.Map<UserDto>(user), tokenPairResult.Value));
        }
        catch (Exception)
        {
            return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.UnknownError);
        }
    }
}
