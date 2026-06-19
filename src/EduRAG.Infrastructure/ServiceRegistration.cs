using EduRAG.Application.Interfaces;
using EduRAG.Application.UseCases;
using EduRAG.Infrastructure.BackgroundJobs;
using EduRAG.Infrastructure.Persistence;
using EduRAG.Infrastructure.Persistence.Queries;
using EduRAG.Infrastructure.Persistence.Repositories;
using EduRAG.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;
using System.Threading.Channels;

namespace EduRAG.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthUseCase>();
        services.AddScoped<ManageClassUseCase>();
        services.AddScoped<ManageSubjectUseCase>();
        services.AddScoped<ManageChapterUseCase>();
        services.AddScoped<UploadMaterialUseCase>();
        services.AddScoped<ChatUseCase>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")!;

        // Single NpgsqlDataSource shared by EF Core and Dapper so UseVector()
        // type mapping is active for all connections, not just EF Core ones.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(cs);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);

        // EF Core + pgvector
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(dataSource, npg => npg.UseVector()));

        // Dapper — connection from the shared data source (has Vector type registered)
        services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

        // Repositories (EF Core — writes)
        services.AddScoped<IClassRepository,          ClassRepository>();
        services.AddScoped<ISubjectRepository,        SubjectRepository>();
        services.AddScoped<IChapterRepository,        ChapterRepository>();
        services.AddScoped<IStudyMaterialRepository,  StudyMaterialRepository>();
        services.AddScoped<IMaterialChunkRepository,  MaterialChunkRepository>();
        services.AddScoped<IChatRepository,           ChatRepository>();
        services.AddScoped<IUserRepository,           UserRepository>();

        // Queries (Dapper — reads)
        services.AddScoped<IClassQueries,    ClassQueries>();
        services.AddScoped<ISubjectQueries,  SubjectQueries>();
        services.AddScoped<IChapterQueries,  ChapterQueries>();
        services.AddScoped<IMaterialQueries, MaterialQueries>();
        services.AddScoped<IChatQueries,     ChatQueries>();
        services.AddScoped<IUserQueries,     UserQueries>();

        // AI
        services.AddHttpClient<IAIService, OllamaAIService>(c =>
            c.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434"));
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<PdfProcessingService>();
        services.AddScoped<VectorizationProcessor>();

        // File storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Background queue (Channel must be Singleton)
        services.AddSingleton(Channel.CreateUnbounded<Guid>());
        services.AddHostedService<VectorizationWorker>();
        services.AddHostedService<PendingMaterialsRequeueService>();

        // JWT service
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}
