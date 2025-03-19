using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs.SingleIdPivotEntities;
using AutoMapper;
using Domain.Base;
using Domain.Entities.SingleIdPivotEntities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Endpoint.SignalRHub
{
    public interface IUserLearningService
    {
        Task<IEnumerable<LearningDto>> GetUserLearnings(Guid userId);
    }

    public class UserLearningService : IUserLearningService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserLearningService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LearningDto>> GetUserLearnings(Guid userId)
        {
            var learnings = await _unitOfWork.Repository<Learning>().FindMany(
                new BaseSpecification<Learning>(l => l.UserId == userId).AddInclude(query => query.Include(l => l.Knowledge!).Include(l => l.LearningHistories))
            );

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_mapper.Map<List<LearningDto>>(learnings), options);
            return JsonSerializer.Deserialize<IEnumerable<LearningDto>>(json, options) ?? Enumerable.Empty<LearningDto>();
        }
    }
}