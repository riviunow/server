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

public class LoginParams
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class LoginUseCase : IUseCase<(UserDto, JWTPairResponse), LoginParams>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly GenerateTokenPairUseCase _generateTokenPairUseCase;
    private readonly IConfiguration _configuration;

    public LoginUseCase(IUnitOfWork unitOfWork, IMapper mapper, GenerateTokenPairUseCase generateTokenPairUseCase, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _generateTokenPairUseCase = generateTokenPairUseCase;
        _configuration = configuration;
    }

    public async Task<Result<(UserDto, JWTPairResponse)>> Execute(LoginParams parameters)
    {
        try
        {
            var userRepository = _unitOfWork.Repository<User>();
            var user = await userRepository.Find(
                new BaseSpecification<User>(u => u.Email == parameters.Email)
                .AddInclude(query => query.Include(u => u.Authentication!)));

            if (user == null)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.UserNotFoundWithEmail);
            else if (!user.Authentication!.VerifyPassword(parameters.Password))
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.WrongPassword);
            else if (!user.Authentication!.IsEmailConfirmed)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.EmailNotConfirmed);
            else if (!user.Authentication!.IsActivated)
                return Result<(UserDto, JWTPairResponse)>.Fail(ErrorMessage.AccountIsLocked);

            var tokenPairResult = await _generateTokenPairUseCase.Execute(user);
            if (!tokenPairResult.IsSuccess)
                return Result<(UserDto, JWTPairResponse)>.Fail(tokenPairResult.Error);
            var tokenPair = tokenPairResult.Value;

            var authentication = user.Authentication;
            authentication.User = null;
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
