using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Profile;
using Domain.Interfaces;
using Shared.Types;

namespace Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly GetProfileUseCase _getProfileUseCase;
        private readonly UpdateProfileUseCase _updateProfileUseCase;
        private readonly DeleteAccountUseCase _deleteAccountUseCase;


        public ProfileService(GetProfileUseCase getProfileUseCase, UpdateProfileUseCase updateProfileUseCase, DeleteAccountUseCase deleteAccountUseCase)
        {
            _getProfileUseCase = getProfileUseCase;
            _updateProfileUseCase = updateProfileUseCase;
            _deleteAccountUseCase = deleteAccountUseCase;
        }

        public Task<Result<UserDto>> DeleteAccount()
        {
            return _deleteAccountUseCase.Execute(NoParam.Value);
        }

        public Task<Result<UserDto>> GetProfile()
        {
            return _getProfileUseCase.Execute(NoParam.Value);
        }

        public Task<Result<UserDto>> UpdateProfile(UpdateProfileParams Params)
        {
            return _updateProfileUseCase.Execute(Params);
        }
    }
}