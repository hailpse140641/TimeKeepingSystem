namespace BusinessObject.DTO
{
    public class WorkslotEmployeeDTO
    {
        public Guid? workslotEmployeeId { get; set; }
        public Guid? attendanceStatusMorningId { get; set; }
        public Guid? attendanceStatusAfternoonId { get; set; } 
        public Guid? workslotEmployeeMorningId { get; set; }
        public DateTime? Date { get; set; }
        public string? CheckInTime { get; set; }
        public string? CheckOutTime { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? WorkslotId { get; set; }
        public Guid? AttendanceStatusId { get; set; }
        public bool IsDeleted { get; set; }
        public string? TimeLeaveEarly { get; set; }
        public string? TimeComeLate { get; set; }
        public Guid? deciderId { get; set; }
        public string? deciderName { get; set; }
        public string? SlotStart { get; set; }
        public string? SlotEnd { get; set; }
        public string? statusName { get; set; }
        public string? reason { get; set; }
        public string? linkFile { get; set; }
        public Guid? RequestId { get; set; }
    }
}