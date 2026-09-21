using Service.Application.DTOs.Ai;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Account;
using Service.Domain.Ai;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class AiService : IAiService
    {
        private readonly IAiConnectorClient _aiConnectorClient;
        private readonly IUserRepository _userRepository;
        private readonly ISettingRepository _settingRepository;
        private readonly ICurrentUserService _currentUserService;

        public AiService(
            IAiConnectorClient aiConnectorClient,
            IUserRepository userRepository,
            ISettingRepository settingRepository,
            ICurrentUserService currentUserService)
        {
            _aiConnectorClient = aiConnectorClient;
            _userRepository = userRepository;
            _settingRepository = settingRepository;
            _currentUserService = currentUserService;
        }

        public async Task<AskResponseDto> AskAsync(string question, CancellationToken cancellationToken = default)
        {
            var (workspaceId, threadId) = await ResolveWorkspaceAndThreadAsync(cancellationToken);
            var answer = await _aiConnectorClient.AskAsync(workspaceId, threadId, question, cancellationToken);
            return new AskResponseDto { Answer = answer };
        }

        public async Task<List<AiChatMessage>> GetChatHistoryAsync(CancellationToken cancellationToken = default)
        {
            var (workspaceId, threadId) = await ResolveWorkspaceAndThreadAsync(cancellationToken);
            return await _aiConnectorClient.GetChatHistoryAsync(workspaceId, threadId, cancellationToken);
        }

        private async Task<(string WorkspaceId, string ThreadId)> ResolveWorkspaceAndThreadAsync(CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken);
            if (string.IsNullOrEmpty(user?.ThreadId))
                throw new BadRequestException("User does not have a thread configured.");

            var setting = await _settingRepository.GetByIdAsync(1, cancellationToken);
            if (string.IsNullOrEmpty(setting?.WorkspaceId))
                throw new BadRequestException("Workspace is not configured.");

            return (setting.WorkspaceId, user.ThreadId);
        }
    }
}
