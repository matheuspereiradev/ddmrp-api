namespace Service.Application.DTOs.Setting
{
    public class SettingPutDto
    {
        public bool MondayIsWorkingDay { get; set; }
        public bool TuesdayIsWorkingDay { get; set; }
        public bool WednesdayIsWorkingDay { get; set; }
        public bool ThursdayIsWorkingDay { get; set; }
        public bool FridayIsWorkingDay { get; set; }
        public bool SaturdayIsWorkingDay { get; set; }
        public bool SundayIsWorkingDay { get; set; }
    }
}
