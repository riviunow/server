using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

using Domain.Interfaces;
using Infrastructure.Data;
using Vocab.Infrastructure.Data;
using Application.Mappings;
using Application.Interfaces;
using Application.Services;
using Application.UseCases.JWT;
using Application.UseCases.Auth;
using Application.UseCases.Tracks;
using Application.UseCases.Subjects;
using Application.UseCases.Knowledges.KnowledgeTypes;
using Application.Interfaces.Knowledges;
using Application.Services.Knowledges;
using Application.UseCases.Knowledges.KnowledgeTopics;
using Application.UseCases.Knowledges;
using Application.UseCases.Knowledges.Learnings;
using Application.UseCases.Games;
using Application.Interfaces.Games;
using Application.Services.Games;
using Application.UseCases.Games.GameOptions;
using Application.Interfaces.Games.GameOptions;
using Application.UseCases.Knowledges.LearningLists;
using Application.UseCases.Knowledges.PublicationRequests;
using Application.UseCases.Profile;

namespace Endpoint.Extensions;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfiguration = configuration.GetConnectionString("RedisConnection");

        if (string.IsNullOrEmpty(redisConfiguration))
        {
            throw new ArgumentNullException(nameof(redisConfiguration), "Redis connection string cannot be null or empty.");
        }

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfiguration));

        services.AddScoped<IRedisCache, RedisCache>();

        services.AddScoped<IFileStorageService, FileStorageService>();

        services.AddScoped<IMailService, MailService>();

        services.AddAutoMapper(typeof(MappingProfile));

        services.AddScoped<GenerateTokenPairUseCase>();
        services.AddScoped<RenewTokenPairUseCase>();
        services.AddScoped<IJWTService, JwtService>();

        services.AddScoped<LoginUseCase>();
        services.AddScoped<RegisterUseCase>();
        services.AddScoped<ConfirmRegistrationEmailUseCase>();
        services.AddScoped<ForgotPasswordUseCase>();
        services.AddScoped<ResendCodeUseCase>();
        services.AddScoped<ConfirmPasswordResettingEmailUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<GetProfileUseCase>();
        services.AddScoped<UpdateProfileUseCase>();
        services.AddScoped<DeleteAccountUseCase>();
        services.AddScoped<IProfileService, ProfileService>();

        services.AddScoped<CreateDeleteTrackSubjectUseCase>();
        services.AddScoped<GetDetailedTracksUseCase>();
        services.AddScoped<GetTracksUseCase>();
        services.AddScoped<GetTrackByGuidUseCase>();
        services.AddScoped<CreateTrackUseCase>();
        services.AddScoped<UpdateTrackUseCase>();
        services.AddScoped<DeleteTrackUseCase>();
        services.AddScoped<ITrackService, TrackService>();

        services.AddScoped<CreateDeleteSubjectKnowledgeUseCase>();
        services.AddScoped<CreateSubjectUseCase>();
        services.AddScoped<DeleteSubjectUseCase>();
        services.AddScoped<GetSubjectByGuidUseCase>();
        services.AddScoped<GetSubjectsUseCase>();
        services.AddScoped<UpdateSubjectUseCase>();
        services.AddScoped<ISubjectService, SubjectService>();

        services.AddScoped<CreateKnowledgeTypeUseCase>();
        services.AddScoped<DeleteKnowledgeTypeUseCase>();
        services.AddScoped<GetKnowledgeTypeByGuidUseCase>();
        services.AddScoped<GetKnowledgeTypesUseCase>();
        services.AddScoped<UpdateKnowledgeTypeUseCase>();
        services.AddScoped<Application.UseCases.Knowledges.KnowledgeTypes.AttachDetachKnowledgesUseCase>();
        services.AddScoped<IKnowledgeTypeService, KnowledgeTypeService>();

        services.AddScoped<CreateKnowledgeTopicUseCase>();
        services.AddScoped<DeleteKnowledgeTopicUseCase>();
        services.AddScoped<GetKnowledgeTopicByGuidUseCase>();
        services.AddScoped<GetKnowledgeTopicsUseCase>();
        services.AddScoped<UpdateKnowledgeTopicUseCase>();
        services.AddScoped<Application.UseCases.Knowledges.KnowledgeTopics.AttachDetachKnowledgesUseCase>();
        services.AddScoped<GetTopicsForMigrationUseCase>();
        services.AddScoped<IKnowledgeTopicService, KnowledgeTopicService>();

        services.AddScoped<SearchKnowledgesUseCase>();
        services.AddScoped<GetKnowledgesUseCase>();
        services.AddScoped<GetDetailedKnowledgeByGuidUseCase>();
        services.AddScoped<GetCreatedKnowledgesUseCase>();
        services.AddScoped<CreateKnowledgeUseCase>();
        services.AddScoped<UpdateKnowledgeUseCase>();
        services.AddScoped<DeleteKnowledgeUseCase>();
        services.AddScoped<GetKnowledgesToLearnUseCase>();
        services.AddScoped<MigrateKnowledgesUseCase>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();

        services.AddScoped<LearnKnowledgeUseCase>();
        services.AddScoped<GetLearningsToReviewUseCase>();
        services.AddScoped<ReviewLearningUseCase>();
        services.AddScoped<GetCurrentUserLearningsUseCase>();
        services.AddScoped<GetUnlistedLearningsUseCase>();
        services.AddScoped<ILearningService, LearningService>();

        services.AddScoped<CreateGameUseCase>();
        services.AddScoped<DeleteGameUseCase>();
        services.AddScoped<GetGameByGuidUseCase>();
        services.AddScoped<GetAllGamesUseCase>();
        services.AddScoped<UpdateGameUseCase>();
        services.AddScoped<AttachGameToKnowledgeUseCase>();
        services.AddScoped<IGameService, GameService>();

        services.AddScoped<CreateGameOptionUseCase>();
        services.AddScoped<DeleteGameOptionUseCase>();
        services.AddScoped<CreateGroupedGameOptionsUseCase>();
        services.AddScoped<UpdateGameOptionUseCase>();
        services.AddScoped<IGameOptionService, GameOptionService>();

        services.AddScoped<CreateLearningListUseCase>();
        services.AddScoped<DeleteLearningListUseCase>();
        services.AddScoped<GetLearningListByGuidUseCase>();
        services.AddScoped<GetAllLearningListsUseCase>();
        services.AddScoped<UpdateLearningListUseCase>();
        services.AddScoped<AddRemoveKnowledgesToLearningListUseCase>();
        services.AddScoped<ILearningListService, LearningListService>();

        services.AddScoped<RequestPublishKnowledgeUseCase>();
        services.AddScoped<DeletePublicationRequestUseCase>();
        services.AddScoped<GetPublicationRequestsUseCase>();
        services.AddScoped<ApproveRejectPublicationRequestUseCase>();
        services.AddScoped<UpdateKnowledgeVisibilityUseCase>();
        services.AddScoped<IPublicationRequestService, PublicationRequestService>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        var connectionString = configuration.GetConnectionString("MySqlConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Mysql connection string cannot be null or empty.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        return services;
    }
}
