using Domain.Base;
using Domain.Entities.SingleIdEntities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Constants;
using Shared.Types;

namespace Application.UseCases.JWT;

public class RenewTokenPairUseCase : IUseCase<JWTPairResponse, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GenerateTokenPairUseCase _generateTokenPairUseCase;
    private readonly IConfiguration _configuration;

    public RenewTokenPairUseCase(IUnitOfWork unitOfWork, GenerateTokenPairUseCase generateTokenPairUseCase, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _generateTokenPairUseCase = generateTokenPairUseCase;
        _configuration = configuration;
    }

    public async Task<Result<JWTPairResponse>> Execute(string refreshToken)
    {
        try
        {
            var authentication = await _unitOfWork
                .Repository<Authentication>().Find(
                    new BaseSpecification<Authentication>(a => a.RefreshToken == refreshToken)
                    .AddInclude(query => query.Include(a => a.User!)));

            var user = authentication?.User;

            if (authentication == null || user == null)
            {
                return Result<JWTPairResponse>.Fail(ErrorMessage.UserNotFound);
            }
            else if (authentication.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return Result<JWTPairResponse>.Fail(ErrorMessage.RefreshTokenIsExpired);
            }

            Result<JWTPairResponse> tokenPairResult = await _generateTokenPairUseCase.Execute(user);
            var tokenPair = tokenPairResult.Value;

            authentication.User = null;
            authentication.RefreshToken = tokenPair.RefreshToken;
            authentication.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]));
            await _unitOfWork.Repository<Authentication>().Update(authentication);

            return tokenPairResult;
        }
        catch
        {
            return Result<JWTPairResponse>.Fail(ErrorMessage.UnknownError);
        }
    }


}
