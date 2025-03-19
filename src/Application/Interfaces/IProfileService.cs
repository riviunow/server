using Application.DTOs;
using Application.UseCases.Profile;
using Shared.Types;

namespace Application.Interfaces
{
    public interface IProfileService
    {
        /// <summary>
        /// Get the profile of the authenticated user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ErrorMessage.UserNotFound">User not found</exception>
        Task<Result<UserDto>> GetProfile();

        /// <summary>
        /// Update the profile of the authenticated user
        /// </summary>
        /// <param name="Params"></param>
        /// <returns></returns>
        /// <exception cref="ErrorMessage.UserNotFound">User not found</exception>
        /// <exception cref="ErrorMessage.DeleteFileError">Error deleting file</exception>
        /// <exception cref="ErrorMessage.StoreFileError">Error storing file</exception>
        Task<Result<UserDto>> UpdateProfile(UpdateProfileParams Params);

        /// <summary>
        /// Delete the account of the authenticated user
        /// </summary>
        /// <returns>return the deleted user</returns>
        /// <exception cref="ErrorMessage.UserNotFound">User not found</exception>
        Task<Result<UserDto>> DeleteAccount();

    }
}