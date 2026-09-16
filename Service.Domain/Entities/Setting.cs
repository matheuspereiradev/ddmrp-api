namespace Service.Domain.Entities
{
    public class Setting : BaseEntity
    {
        public bool MondayIsWorkingDay { get; set; } = true;
        public bool TuesdayIsWorkingDay { get; set; } = true;
        public bool WednesdayIsWorkingDay { get; set; } = true;
        public bool ThursdayIsWorkingDay { get; set; } = true;
        public bool FridayIsWorkingDay { get; set; } = true;
        public bool SaturdayIsWorkingDay { get; set; } = false;
        public bool SundayIsWorkingDay { get; set; } = false;
    }
}
