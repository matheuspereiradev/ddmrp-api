using Service.Application.DTOs.Setting;

namespace Service.Application.Interfaces
{
    public interface ISettingService
    {
        Task<SettingGetDto> GetAsync(CancellationToken cancellationToken = default);
        Task<SettingGetDto> UpdateWorkingDaysAsync(SettingPutDto putDto, CancellationToken cancellationToken = default);
    }
}
