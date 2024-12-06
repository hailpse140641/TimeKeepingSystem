using BusinessObject.DTO;
using DataAccess.InterfaceRepository;
using Microsoft.Data.SqlClient.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DataAccess.Repository
{
    public class RequestLeaveRepository : Repository<RequestLeave>, IRequestLeaveRepository
    {
        private readonly MyDbContext _dbContext;
        private readonly IEmployeeRepository _employeeRepository;

        public RequestLeaveRepository(MyDbContext context, IEmployeeRepository employeeRepository) : base(context)
        {
            // You can add more specific methods here if needed
            _dbContext = context;
            _employeeRepository = employeeRepository;
        }

        public async Task<List<RequestLeaveDTO>> GetAllAsync()
        {
            var ass = await base.GetAllAsync();
            return await ass.Select(a => new RequestLeaveDTO
            {
                Id = a.Id,
                Name = a.Name,
                LeaveTypeId = a.LeaveTypeId,
                IsDeleted = a.IsDeleted
            }).ToListAsync();
        }

        public async Task<bool> AddAsync(RequestLeaveDTO a)
        {
            try
            {
                await base.AddAsync(new RequestLeave() // have dbSaveChange inside method
                {
                    Id = a.Id,
                    Name = a.Name,
                    LeaveTypeId = a.LeaveTypeId,
                    IsDeleted = a.IsDeleted
                });
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            try
            {
                await base.SoftDeleteAsync(id);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public object GetRequestLeaveOfEmployeeById(Guid employeeId)
        {

            var employee = _dbContext.Employees.Where(e => e.IsDeleted == false && e.Id == employeeId).FirstOrDefault();
            if (employee == null)
            {
                throw new Exception("Employee not found");
            }
            var result = new List<LeaveRequestDTO>();
            var list = _dbContext.Requests.Include(r => r.RequestLeave).ThenInclude(rl => rl.LeaveType).Include(r => r.RequestLeave).ThenInclude(rl => rl.WorkslotEmployees).ThenInclude(we => we.Workslot).Where(r => r.IsDeleted == false)
                 .Where(r => r.EmployeeSendRequestId == employeeId && r.requestType == RequestType.Leave)
                 .ToList();
            list.ForEach(r =>
            {
                List<DateRangeDTO> dateRange = null;
                dateRange = r.RequestLeave.WorkslotEmployees.Where(r => r.IsDeleted == false).Select(we => new DateRangeDTO() { title = we.Workslot.DateOfSlot.ToString("yyyy/MM/dd"), type = we.Workslot.IsMorning ? "Morning" : "Afternoon" }).ToList().OrderBy(s => DateTime.ParseExact(s.title, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToList();
                dateRange = MergeToFullDay(dateRange);
                result.Add(new LeaveRequestDTO()
                {
                    id = r.Id,
                    employeeId = employeeId,
                    employeeName = employee.FirstName + " " + employee.LastName,
                    employeeNumber = employee.EmployeeNumber,
                    startDate = r.RequestLeave.FromDate.ToString("yyyy/MM/dd"),
                    endDate = r.RequestLeave.ToDate.ToString("yyyy/MM/dd"),
                    leaveTypeId = r.RequestLeave.LeaveTypeId,
                    leaveType = r.RequestLeave.LeaveType.Name,
                    status = (int)r.Status,
                    statusName = r.Status.ToString(),
                    reason = r.Reason,
                    reasonReject = r.Message,
                    linkFile = r.PathAttachmentFile,
                    dateRange = dateRange,
                    deciderId = r.EmployeeIdLastDecider,
                });
            });

            return result;
        }

        public List<DateRangeDTO> getDateRangeOfRequestLeave(RequestLeave requestLeave)
        {
            List<DateRangeDTO> dateRange = null;
            dateRange = requestLeave.WorkslotEmployees.Where(r => r.IsDeleted == false).Select(we => new DateRangeDTO() { title = we.Workslot.DateOfSlot.ToString("yyyy/MM/dd"), type = we.Workslot.IsMorning ? "Morning" : "Afternoon" }).ToList().OrderBy(s => DateTime.ParseExact(s.title, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToList();
            return MergeToFullDay(dateRange);
        }

        public object GetRequestLeaveAllEmployee(string? nameSearch, int status, Guid? employeeId = null)
        {
            var result = new List<LeaveRequestDTO>();
            var list = _dbContext.Requests
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.LeaveType)
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.WorkslotEmployees)
                        .ThenInclude(we => we.Workslot)
                .Where(r => r.IsDeleted == false && r.requestType == RequestType.Leave);

            // Filter by status if specified
            if (status != -1)
            {
                list = list.Where(r => (int)r.Status == status);
            }

            // Filter by manager's department if employeeId is provided
            Guid? departmentId = null;
            if (employeeId.HasValue)
            {
                var manager = _dbContext.Employees
                    .Include(e => e.UserAccount)
                    .ThenInclude(ua => ua.Role)
                    .FirstOrDefault(e => e.Id == employeeId);

                if (manager != null && manager.UserAccount.Role.Name == "Manager")
                {
                    departmentId = manager.DepartmentId;
                } else
                {
                    throw new Exception("EmployeeId is not Manager");
                }
            }

            if (departmentId.HasValue)
            {
                list = list.Where(r => r.EmployeeSendRequest.DepartmentId == departmentId);
            }

            list.ToList().ForEach(r =>
            {
                var employee = _dbContext.Employees.Where(e => e.IsDeleted == false && e.Id == r.EmployeeSendRequestId).FirstOrDefault(); // Directly get employee send request from the navigation property
                if (employee == null) return;

                List<DateRangeDTO> dateRange = null;
                dateRange = r.RequestLeave.WorkslotEmployees.Where(r => r.IsDeleted == false).Select(we => new DateRangeDTO() { title = we.Workslot.DateOfSlot.ToString("yyyy/MM/dd"), type = we.Workslot.IsMorning ? "Morning" : "Afternoon" }).ToList().OrderBy(s => DateTime.ParseExact(s.title, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToList();

                dateRange = MergeToFullDay(dateRange);

                result.Add(new LeaveRequestDTO
                {
                    id = r.Id,
                    employeeId = employee.Id,
                    employeeName = $"{employee.FirstName} {employee.LastName}",
                    employeeNumber = employee.EmployeeNumber,
                    submitDate = r.SubmitedDate.ToString("yyyy/MM/dd"),
                    startDate = r.RequestLeave.FromDate.ToString("yyyy/MM/dd"),
                    endDate = r.RequestLeave.ToDate.ToString("yyyy/MM/dd"),
                    leaveTypeId = r.RequestLeave.LeaveTypeId,
                    leaveType = r.RequestLeave.LeaveType.Name,
                    status = (int)r.Status,
                    statusName = r.Status.ToString(),
                    reason = r.Reason,
                    reasonReject = r.Message,
                    linkFile = r.PathAttachmentFile,
                    numberOfLeaveDate = CountNumOfLeaveDate(dateRange),
                    deciderId = r.EmployeeIdLastDecider,
                });
            });

            if (!string.IsNullOrEmpty(nameSearch))
            {
                result = result.Where(r => r.employeeName.ToLower().Contains(nameSearch.ToLower())).ToList();
            }

            return result;
        }

        public object GetRequestLeaveAllEmployeeInHrTeam(string? nameSearch, int status, Guid? employeeId = null)
        {
            var result = new List<LeaveRequestDTO>();
            var list = _dbContext.Requests
                .Include(r => r.EmployeeSendRequest)
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.LeaveType)
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.WorkslotEmployees)
                        .ThenInclude(we => we.Workslot)
                .Where(r => r.IsDeleted == false && r.requestType == RequestType.Leave)
                .Where(r => r.EmployeeSendRequest.DepartmentId == Guid.Parse("d4a6ec67-3d4e-4e5c-8fe3-64d631f27ab0"));

            // Filter by status if specified
            if (status != -1)
            {
                list = list.Where(r => (int)r.Status == status);
            }

            // Filter by manager's department if employeeId is provided
            Guid? departmentId = null;
            if (employeeId.HasValue)
            {
                var manager = _dbContext.Employees
                    .Include(e => e.UserAccount)
                    .ThenInclude(ua => ua.Role)
                    .FirstOrDefault(e => e.Id == employeeId);

                if (manager != null && manager.UserAccount.Role.Name == "Manager")
                {
                    departmentId = manager.DepartmentId;
                }
                else
                {
                    throw new Exception("EmployeeId is not Manager");
                }
            }

            if (departmentId.HasValue)
            {
                list = list.Where(r => r.EmployeeSendRequest.DepartmentId == departmentId);
            }

            list.ToList().ForEach(r =>
            {
                var employee = _dbContext.Employees.Where(e => e.IsDeleted == false && e.Id == r.EmployeeSendRequestId).FirstOrDefault(); // Directly get employee send request from the navigation property
                if (employee == null) return;

                List<DateRangeDTO> dateRange = null;
                dateRange = r.RequestLeave.WorkslotEmployees.Where(r => r.IsDeleted == false).Select(we => new DateRangeDTO() { title = we.Workslot.DateOfSlot.ToString("yyyy/MM/dd"), type = we.Workslot.IsMorning ? "Morning" : "Afternoon" }).ToList().OrderBy(s => DateTime.ParseExact(s.title, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToList();

                dateRange = MergeToFullDay(dateRange);

                result.Add(new LeaveRequestDTO
                {
                    id = r.Id,
                    employeeId = employee.Id,
                    employeeName = $"{employee.FirstName} {employee.LastName}",
                    employeeNumber = employee.EmployeeNumber,
                    submitDate = r.SubmitedDate.ToString("yyyy/MM/dd"),
                    startDate = r.RequestLeave.FromDate.ToString("yyyy/MM/dd"),
                    endDate = r.RequestLeave.ToDate.ToString("yyyy/MM/dd"),
                    leaveTypeId = r.RequestLeave.LeaveTypeId,
                    leaveType = r.RequestLeave.LeaveType.Name,
                    status = (int)r.Status,
                    statusName = r.Status.ToString(),
                    reason = r.Reason,
                    reasonReject = r.Message,
                    linkFile = r.PathAttachmentFile,
                    numberOfLeaveDate = CountNumOfLeaveDate(dateRange),
                    deciderId = r.EmployeeIdLastDecider,
                });
            });

            if (!string.IsNullOrEmpty(nameSearch))
            {
                result = result.Where(r => r.employeeName.ToLower().Contains(nameSearch.ToLower())).ToList();
            }

            return result;
        }

        public async Task<bool> EditRequestLeave(LeaveRequestDTO dto, Guid employeeId)
        {
            // Find the existing request leave by its ID
            var existingRequest = _dbContext.Requests
                                            .Include(r => r.RequestLeave)
                                            .ThenInclude(rl => rl.LeaveType)
                                            .Include(r => r.RequestLeave)
                                            .ThenInclude(rl => rl.WorkslotEmployees)
                                            .FirstOrDefault(r => r.Id == dto.id && r.IsDeleted == false);

            // If no matching request leave is found, return false
            if (existingRequest == null)
            {
                return false;
            }

            // Update fields based on the provided DTO
            if (dto.startDate != null)
            {
                existingRequest.RequestLeave.FromDate = DateTime.ParseExact(dto.startDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            }

            if (dto.endDate != null)
            {
                existingRequest.RequestLeave.ToDate = DateTime.ParseExact(dto.endDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            }

            if (dto.leaveTypeId != null)
            {
                existingRequest.RequestLeave.LeaveTypeId = (Guid)dto.leaveTypeId;
                var lt = _dbContext.LeaveTypes.Where(lt => lt.IsDeleted == false && lt.Id == dto.leaveTypeId).FirstOrDefault();
                existingRequest.RequestLeave.LeaveType = lt;
            }

            if (dto.status.HasValue)
            {
                if (dto.status.Value == 0)
                {
                    existingRequest.Status = RequestStatus.Pending;
                }
                else
                if (dto.status.Value == 1)
                {
                    existingRequest.Status = RequestStatus.Approved;
                }
                else
                {
                    existingRequest.Status = RequestStatus.Rejected;
                }
            }

            if (dto.reason != null)
            {
                existingRequest.Reason = dto.reason;
            }

            if (dto.reasonReject != null)
            {
                existingRequest.Message = dto.reasonReject;
            }

            if (dto.linkFile != null)
            {
                existingRequest.PathAttachmentFile = dto.linkFile;
            }
            //var employee = _dbContext.Employees.Include(e => e.Team).ThenInclude(d => d.WorkTrackSetting).ThenInclude(wts => wts.WorkDateSetting).Where(e => e.Id == employeeId).FirstOrDefault();
            // Update the WorkslotEmployees if needed. This assumes that the WorkslotEmployees are uniquely identified by their 'title' (date) and 'type' (morning/afternoon)
            List<WorkslotEmployee> newItems = new List<WorkslotEmployee>();

            // Get the current list of WorkslotEmployees that are not deleted.
            List<WorkslotEmployee> currentItems = existingRequest.RequestLeave.WorkslotEmployees.Where(we => we.IsDeleted == false).ToList();

            //if (dto.dateRange != null)
            //{
            //    HandleFullday(dto.dateRange);
            //    // Add all new WorkslotEmployees based on the new dateRange
            //    foreach (var dateRange in dto.dateRange)
            //    {
            //        var workslotToAdd = _dbContext.WorkslotEmployees.Include(we => we.Workslot)
            //                                      .Where(we => we.IsDeleted == false)
            //                                      .Where(we => we.EmployeeId == employeeId);
            //        //foreach (var workslot in workslotToAdd)
            //        //{
            //        //    var test = string.Format("{0:yyyy/MM/dd}", workslot.Workslot.DateOfSlot) == dateRange.title;
            //        //}
            //        var workslotToAdd2 = workslotToAdd.Where(we => we.Workslot.DateOfSlot.Date == DateTime.ParseExact(dateRange.title, "yyyy/MM/dd", CultureInfo.InvariantCulture).Date).Where(we => (we.Workslot.IsMorning ? "Morning" : "Afternoon") == dateRange.type).FirstOrDefault();

            //        if (workslotToAdd2 != null)
            //        {
            //            //existingRequest.RequestLeave.WorkslotEmployees.Add(workslotToAdd2);
            //            newItems.Add(workslotToAdd2);  // Add to newItems list.
            //        }
            //        else
            //        {
            //            var workSlot = _dbContext.Workslots.FirstOrDefault(we => we.DateOfSlot.Date == DateTime.ParseExact(dateRange.title, "yyyy/MM/dd", CultureInfo.InvariantCulture).Date && (we.IsMorning ? "Morning" : "Afternoon") == dateRange.type);
            //            var worktrackSetting = employee.Team.WorkTrackSetting;
            //            if (workSlot == null)
            //            {
            //                workSlot = new Workslot()
            //                {
            //                    Id = Guid.NewGuid(),
            //                    Name = DateTime.ParseExact(dateRange.title, "yyyy/MM/dd", CultureInfo.InvariantCulture).DayOfWeek.ToString(),
            //                    IsMorning = dateRange.type == "Morning" ? true : false,
            //                    DateOfSlot = DateTime.ParseExact(dateRange.title, "yyyy/MM/dd", CultureInfo.InvariantCulture),
            //                    WorkTrackId = worktrackSetting.Id,
            //                    IsDeleted = false
            //                };
            //                await _dbContext.Workslots.AddAsync(workSlot);
            //            }

            //            var newWorkslotEmployee = new WorkslotEmployee()
            //            {
            //                Id = Guid.NewGuid(),
            //                EmployeeId = employeeId,
            //                Employee = employee,
            //                Workslot = workSlot,
            //                WorkslotId = workSlot.Id,
            //                AttendanceStatusId = _dbContext.AttendanceStatuses.FirstOrDefault(att => att.LeaveTypeId != null && att.LeaveTypeId == existingRequest.RequestLeave.LeaveTypeId).Id,
            //                AttendanceStatus = _dbContext.AttendanceStatuses.FirstOrDefault(att => att.LeaveTypeId != null && att.LeaveTypeId == existingRequest.RequestLeave.LeaveTypeId),
            //                IsDeleted = false
            //            };
            //            await _dbContext.WorkslotEmployees.AddAsync(newWorkslotEmployee);
            //            newItems.Add(newWorkslotEmployee);
            //        }
            //    }

            //    var itemsToRemove = currentItems.Except(newItems).ToList();

            //    // Remove items.
            //    foreach (var itemToRemove in itemsToRemove)
            //    {
            //        existingRequest.RequestLeave.WorkslotEmployees.Remove(itemToRemove);
            //        var attendanceStatus = _dbContext.AttendanceStatuses.Include(att => att.WorkingStatus).FirstOrDefault(att => att.WorkingStatus.Name == "Not Work Yet");
            //        if (attendanceStatus == null)
            //        {
            //            attendanceStatus = new AttendanceStatus()
            //            {
            //                Id = Guid.NewGuid(),
            //                IsDeleted = false,
            //                WorkingStatus = _dbContext.WorkingStatuses.Where(ws => ws.Name == "Not Work Yet").FirstOrDefault(),
            //                WorkingStatusId = _dbContext.WorkingStatuses.Where(ws => ws.Name == "Not Work Yet").FirstOrDefault().Id
            //            };
            //        }
            //        itemToRemove.AttendanceStatus = attendanceStatus;
            //        itemToRemove.AttendanceStatusId = attendanceStatus.Id;
            //    }

            //    // Add new items.
            //    foreach (var newItem in newItems)
            //    {
            //        if (!currentItems.Contains(newItem))
            //        {
            //            var attendanceStatus = _dbContext.AttendanceStatuses.Include(att => att.LeaveType).FirstOrDefault(att => att.LeaveTypeId == dto.leaveTypeId);
            //            if (attendanceStatus == null)
            //            {
            //                attendanceStatus = new AttendanceStatus()
            //                {
            //                    Id = Guid.NewGuid(),
            //                    IsDeleted = false,
            //                    LeaveType = _dbContext.LeaveTypes.Where(lt => lt.Id == existingRequest.RequestLeave.LeaveTypeId).FirstOrDefault(),
            //                    LeaveTypeId = existingRequest.RequestLeave.LeaveTypeId
            //                };
            //            }
            //            newItem.AttendanceStatus = attendanceStatus;
            //            newItem.AttendanceStatusId = attendanceStatus.Id;
            //            existingRequest.RequestLeave.WorkslotEmployees.Add(newItem);
            //        }
            //    }
            //}

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<WorkDateSettingDTO> GetWorkDateSettingFromEmployeeId(Guid employeeId)
        {
            var employee = _dbContext.Employees.Include(e => e.Department).ThenInclude(d => d.WorkTrackSetting).ThenInclude(wts => wts.WorkDateSetting).Where(e => e.Id == employeeId).FirstOrDefault();
            if (employee != null)
            {
                var workdatesetting = employee.Department.WorkTrackSetting.WorkDateSetting;
                if (workdatesetting != null)
                {
                    return new WorkDateSettingDTO()
                    {
                        Id = workdatesetting.Id,
                        DateStatus = JsonSerializer.Deserialize<DateStatusDTO>(workdatesetting.DateStatus),
                        IsDeleted = workdatesetting.IsDeleted
                    };
                }
                else
                {
                    return null;
                }

            }
            else
            {
                throw new Exception("Employee Not Exist");
            }
        }

        public async Task<object> CreateRequestLeave(LeaveRequestDTO dto, Guid employeeId)
        {
            if (dto.dateRange == null || !dto.dateRange.Any())
            {
                throw new Exception("You chose wrong date or not chose date yet");
            }

            RequestLeave newRequestLeave = new RequestLeave()
            {
                Id = Guid.NewGuid(),
                FromDate = DateTime.ParseExact(dto.startDate, "yyyy/MM/dd", CultureInfo.InvariantCulture),
                ToDate = DateTime.ParseExact(dto.endDate, "yyyy/MM/dd", CultureInfo.InvariantCulture),
                LeaveTypeId = (Guid)dto.leaveTypeId,
                IsDeleted = false,
                Name = dto.Name ?? "",
                SupportEmployeeId = dto.SupportEmployeeId,
            };

            Employee employeeSendRequest = _dbContext.Employees.Include(e => e.Department).FirstOrDefault(e => e.Id == employeeId);

            // Assuming employeeSendRequest is an object of type Employee
            var departmentId = employeeSendRequest.DepartmentId;
            Guid? managerId = null;
            if (departmentId != null)
            {
                var manager = _dbContext.Employees
                    .Include(e => e.UserAccount)
                    .ThenInclude(ua => ua.Role)
                    .Where(e => e.DepartmentId == departmentId && e.UserAccount.Role.Name == "Manager")
                    .FirstOrDefault();

                // Now manager will be null if there is no manager in the department
                managerId = manager?.Id; // This will be null if no manager is found

                // Use managerId as needed
            }

            if (employeeSendRequest == null)
            {
                throw new Exception("Employee Send Request Not Found");
            }

            // Step 1: Create a new Request and populate its fields
            Request newRequest = new Request()
            {
                Id = Guid.NewGuid(),
                EmployeeSendRequestId = employeeId,
                EmployeeSendRequest = employeeSendRequest,
                Status = RequestStatus.Pending,  // default status
                IsDeleted = false,
                RequestLeaveId = newRequestLeave.Id,
                RequestLeave = newRequestLeave,
                Message = "",
                PathAttachmentFile = dto.linkFile ?? "",
                Reason = dto.reason ?? "",
                SubmitedDate = DateTime.Now,
                requestType = RequestType.Leave,
                EmployeeIdLastDecider = managerId,
            };

            List<WorkslotEmployee> newWorkslotEmployees = new List<WorkslotEmployee>();
            var employee = _dbContext.Employees.Include(e => e.Department).ThenInclude(d => d.WorkTrackSetting).ThenInclude(wts => wts.WorkDateSetting).Where(e => e.Id == employeeId).FirstOrDefault();
            // Step 3: Add WorkslotEmployees based on dateRange
            if (dto.dateRange != null)
            {
                HandleFullday(dto.dateRange);
                foreach (var dateRange in dto.dateRange)
                {
                    // Similar logic as in EditRequestLeave for adding WorkslotEmployees
                    var workslotToAdd = _dbContext.WorkslotEmployees.Include(we => we.Workslot)
                                                .Where(we => we.IsDeleted == false)
                                                .Where(we => we.EmployeeId == employeeId)
                                                .Where(we => we.Workslot.DateOfSlot.Date == DateTime.ParseExact(dateRange.title, "yyyy/MM/dd", CultureInfo.InvariantCulture).Date)
                                                .Where(we => (we.Workslot.IsMorning ? "Morning" : "Afternoon") == dateRange.type)
                                                .FirstOrDefault();

                    if (workslotToAdd != null)
                    {
                        newWorkslotEmployees.Add(workslotToAdd);
                    }
                    else
                    {
                        throw new Exception(string.Format("Workslot of dateRange title = {0}, type = {1}", dateRange.title, dateRange.type));
                    }
                }
                newRequest.RequestLeave.WorkslotEmployees = new List<WorkslotEmployee>();
                foreach (var newItem in newWorkslotEmployees)
                {
                    newRequest.RequestLeave.WorkslotEmployees.Add(newItem);
                }
            }

            await _dbContext.RequestLeaves.AddAsync(newRequestLeave);
            await _dbContext.Requests.AddAsync(newRequest);
            // Step 4: Save to the database
            await _dbContext.SaveChangesAsync();
            await SendRequestLeaveToManagerFirebase(newRequest.Id);

            return new
            {
                newRequestLeaveId = newRequestLeave.Id,
                newRequestId = newRequest.Id
            };
        }

        public double HandleFullday(List<DateRangeDTO> dateRange)
        {
            List<DateRangeDTO> newDateRange = new List<DateRangeDTO>();
            HashSet<string> existingEntries = new HashSet<string>();
            double numOfLeaveDate = 0;

            foreach (var entry in dateRange)
            {
                string key = entry.title + "-" + entry.type;

                // Skip this entry if it already exists
                if (existingEntries.Contains(key))
                {
                    continue;
                }

                // Add to existing entries
                existingEntries.Add(key);

                if (entry.type == "Full Day")
                {
                    numOfLeaveDate += 1;
                    // Split Fullday into Morning and Afternoon, ensuring there are no duplicates
                    string morningKey = entry.title + "-Morning";
                    string afternoonKey = entry.title + "-Afternoon";

                    if (!existingEntries.Contains(morningKey))
                    {
                        newDateRange.Add(new DateRangeDTO() { title = entry.title, type = "Morning" });
                        existingEntries.Add(morningKey);
                    }

                    if (!existingEntries.Contains(afternoonKey))
                    {
                        newDateRange.Add(new DateRangeDTO() { title = entry.title, type = "Afternoon" });
                        existingEntries.Add(afternoonKey);
                    }
                }
                else
                {
                    numOfLeaveDate += 0.5;
                    newDateRange.Add(entry);
                }
            }

            // Replace the original dateRange with the new one
            dateRange.Clear();
            dateRange.AddRange(newDateRange);

            return numOfLeaveDate;
        }

        public double CountNumOfLeaveDate(List<DateRangeDTO> dateRange)
        {
            HashSet<string> existingEntries = new HashSet<string>();
            double numOfLeaveDate = 0;

            foreach (var entry in dateRange)
            {
                string key = entry.title + "-" + entry.type;

                // Skip this entry if it already exists
                if (existingEntries.Contains(key))
                {
                    continue;
                }

                // Add to existing entries
                existingEntries.Add(key);

                if (entry.type == "Full Day")
                {
                    numOfLeaveDate += 1;
                }
                else
                {
                    numOfLeaveDate += 0.5;
                }
            }

            return numOfLeaveDate;
        }

        public static List<DateRangeDTO> MergeToFullDay(List<DateRangeDTO> dateRanges)
        {
            Dictionary<string, string> dateTypeMap = new Dictionary<string, string>();
            List<DateRangeDTO> mergedDateRanges = new List<DateRangeDTO>();

            foreach (var range in dateRanges)
            {
                if (range.title == null || range.type == null)
                {
                    continue; // Skip any null entries.
                }

                if (dateTypeMap.ContainsKey(range.title))
                {
                    dateTypeMap[range.title] = "Full Day";
                }
                else
                {
                    dateTypeMap[range.title] = range.type;
                }
            }

            foreach (var entry in dateTypeMap)
            {
                mergedDateRanges.Add(new DateRangeDTO { title = entry.Key, type = entry.Value });
            }

            return mergedDateRanges;
        }

        public async Task<object> ApproveRequestAndChangeWorkslotEmployee(Guid requestId, Guid? employeeIdDecider)
        {
            // Step 1: Retrieve the Request by requestId
            var request = await _dbContext.Requests
                                          .Include(r => r.RequestLeave)
                                          .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("Request not found");
            }

            // Update the Request status to Approve
            request.Status = RequestStatus.Approved;
            request.EmployeeIdLastDecider = employeeIdDecider;
            request.Status = RequestStatus.Approved;

            // Step 2: Find all WorkslotEmployees that should be updated
            var fromDate = request.RequestLeave.FromDate;
            var toDate = request.RequestLeave.ToDate;
            var leaveTypeId = request.RequestLeave.LeaveTypeId;

            var workslotEmployees = await _dbContext.WorkslotEmployees
                                                    .Include(we => we.Workslot)
                                                    .Where(we => we.EmployeeId == request.EmployeeSendRequestId)
                                                    .Where(we => we.Workslot.DateOfSlot >= fromDate && we.Workslot.DateOfSlot <= toDate)
                                                    .ToListAsync();

            // Step 3: Update the AttendanceStatus for these WorkslotEmployees
            var newAttendanceStatus = await _dbContext.AttendanceStatuses
                                                      .FirstOrDefaultAsync(att => att.LeaveTypeId == leaveTypeId);

            if (newAttendanceStatus == null)
            {
                throw new Exception("Attendance status for the given LeaveTypeId not found");         }

            foreach (var workslotEmployee in workslotEmployees)
            {
                workslotEmployee.AttendanceStatus = newAttendanceStatus;
                workslotEmployee.AttendanceStatusId = newAttendanceStatus.Id;
            }

            // Step 4: Save changes to the database
            await _dbContext.SaveChangesAsync();
            await SendRequestLeaveToEmployeeFirebase(requestId);

            return new { message = "Request approved and WorkslotEmployee updated successfully" };
        }

        public async Task<object> RejectLeaveRequest(RequestReasonDTO requestObj)
        {
            Guid requestId = requestObj.requestId;
            string reason = requestObj.reason;

            // Retrieve the request by requestId
            var request = await _dbContext.Requests
                                           .Include(r => r.RequestLeave)
                                           .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("RequestId not found.");
            }

            if (request.RequestLeave == null)
            {
                throw new Exception("Request Leave not found.");
            }
            // Check if the leave date is in the past
            if (request.RequestLeave.FromDate.Date < DateTime.Today)
            {
                throw new Exception("Cannot cancel leave for past dates.");
            }

            // Check if the request is indeed approved; only approved requests can be cancelled

            // Set the Request status to Cancelled or Rejected based on your application logic
            request.Status = RequestStatus.Rejected; // Assuming Cancelled is a defined status in your system
            request.Message = reason;
            request.EmployeeIdLastDecider = requestObj.employeeIdDecider;

            // Save the changes to the database
            await _dbContext.SaveChangesAsync();
            await SendRequestLeaveToEmployeeFirebase(requestId);

            return new { message = "Leave request rejected successfully." };
        }

        public async Task<object> CancelApprovedLeaveRequest(RequestReasonDTO requestObj)
        {
            Guid requestId = requestObj.requestId;
            string reason = requestObj.reason;

            // Retrieve the request by requestId
            var request = await _dbContext.Requests
                                           .Include(r => r.RequestLeave)
                                           .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("RequestId not found.");
            }

            if (request.RequestLeave == null)
            {
                throw new Exception("Request Leave not found.");
            }
            // Check if the leave date is in the past
            if (request.RequestLeave.FromDate.Date < DateTime.Today)
            {
                throw new Exception("Cannot cancel leave for past dates.");
            }

            // Check if the request is indeed approved; only approved requests can be cancelled
            if (request.Status != RequestStatus.Approved)
            {
                throw new Exception("Only approved requests can be cancelled.");
            }

            // Set the Request status to Cancelled or Rejected based on your application logic
            request.Status = RequestStatus.Cancel; // Assuming Cancelled is a defined status in your system
            request.Message = reason;
            request.EmployeeIdLastDecider = requestObj.employeeIdDecider;

            // Find all WorkslotEmployees that were updated due to this request and reset their attendance status
            var fromDate = request.RequestLeave.FromDate;
            var toDate = request.RequestLeave.ToDate;

            var workslotEmployees = await _dbContext.WorkslotEmployees
                                                    .Include(we => we.Workslot)
                                                    .Where(we => we.EmployeeId == request.EmployeeSendRequestId && we.Workslot.DateOfSlot >= fromDate && we.Workslot.DateOfSlot <= toDate)
                                                    .ToListAsync();

            // Assuming you have a default or previous attendance status to reset to; adjust this logic as necessary
            var defaultAttendanceStatus = await _dbContext.AttendanceStatuses.Include(ass => ass.WorkingStatus)
                                                           .FirstOrDefaultAsync(att => att.WorkingStatus != null && att.WorkingStatus.Name == "Not Work Yet");

            if (defaultAttendanceStatus == null)
            {
                throw new Exception("Default attendance status not found.");
            }

            foreach (var workslotEmployee in workslotEmployees)
            {
                workslotEmployee.AttendanceStatus = defaultAttendanceStatus;
                workslotEmployee.AttendanceStatusId = defaultAttendanceStatus.Id;
            }

            // Save the changes to the database
            await _dbContext.SaveChangesAsync();
            await SendRequestLeaveToEmployeeFirebase(requestId);

            return new { message = "Leave request cancelled successfully." };
        }

        public object GetRequestLeaveByRequestId(Guid requestId)
        {
            // Find the request by its Id
            var request = _dbContext.Requests
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.LeaveType)
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.WorkslotEmployees)
                    .ThenInclude(we => we.Workslot)
                .Where(r => r.IsDeleted == false && r.Id == requestId && r.requestType == RequestType.Leave)
                .FirstOrDefault();

            if (request == null)
            {
                return null; // Or any other error handling
            }

            // Find the employee details
            var employee = _dbContext.Employees.Where(e => e.IsDeleted == false && e.Id == request.EmployeeSendRequestId).FirstOrDefault();

            List<DateRangeDTO> dateRange = request.RequestLeave.WorkslotEmployees
                .Where(r => r.IsDeleted == false)
                .Select(we => new DateRangeDTO() { title = we.Workslot.DateOfSlot.ToString("yyyy/MM/dd"), type = we.Workslot.IsMorning ? "Morning" : "Afternoon" })
                .OrderBy(s => DateTime.ParseExact(s.title, "yyyy/MM/dd", CultureInfo.InvariantCulture))
                .ToList();

            dateRange = MergeToFullDay(dateRange);

            var result = new LeaveRequestDTO()
            {
                id = request.Id,
                employeeName = employee.FirstName + " " + employee.LastName,
                employeeNumber = employee.PhoneNumber,
                startDate = request.RequestLeave.FromDate.ToString("yyyy/MM/dd"),
                endDate = request.RequestLeave.ToDate.ToString("yyyy/MM/dd"),
                leaveTypeId = request.RequestLeave.LeaveTypeId,
                leaveType = request.RequestLeave.LeaveType.Name,
                status = (int)request.Status,
                statusName = request.Status.ToString(),
                reason = request.Reason,
                linkFile = request.PathAttachmentFile,
                dateRange = dateRange,
            };

            return result;
        }

        public async Task<List<object>> GetApprovedLeaveDaysByTypeAsync(Guid employeeId)
        {
            var result = new List<object>();

            // Fetch all approved leave requests for the given employee
            var approvedLeaveRequests = await _dbContext.Requests
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.LeaveType)
                .Include(r => r.RequestLeave)
                    .ThenInclude(rl => rl.WorkslotEmployees)
                        .ThenInclude(we => we.Workslot)
                .Where(r => r.EmployeeSendRequestId == employeeId && r.Status == RequestStatus.Approved && r.requestType == RequestType.Leave)
                .ToListAsync();

            // Group by leaveTypeId to process each type separately
            var groupedByLeaveType = approvedLeaveRequests
                .GroupBy(r => r.RequestLeave.LeaveTypeId)
                .ToList();

            foreach (var group in groupedByLeaveType)
            {
                var leaveTypeId = group.Key;
                var leaveType = await _dbContext.LeaveTypes.FindAsync(leaveTypeId);
                var leaveTypeName = leaveType?.Name ?? "Unknown Leave Type";

                float totalDays = group.Sum(request =>
                {
                    var dateRanges = MergeToFullDay(
                        request.RequestLeave.WorkslotEmployees
                            .Where(we => !we.IsDeleted)
                            .Select(we => new DateRangeDTO { title = we.Workslot.DateOfSlot.ToString("yyyy/MM/dd"), type = we.Workslot.IsMorning ? "Morning" : "Afternoon" })
                            .ToList()
                    );

                    // Assuming 1 day for full day and 0.5 for half day, adjust your logic here
                    return dateRanges.Sum(dateRange => dateRange.type == "Full Day" ? 1 : 0.5f);
                });

                result.Add(new
                {
                    leaveTypeId = leaveTypeId.ToString(),
                    leaveTypeName = leaveTypeName,
                    numOfDateLeave = totalDays
                });
            }

            return result;
        }

        public async Task<object> DeleteLeaveRequestIfNotApproved(Guid requestId, Guid? employeeIdDecider)
        {
            // Retrieve the request by its Id
            var request = await _dbContext.Requests
                                           .Include(r => r.RequestLeave)
                                           .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("Leave request not found.");
            }

            // Check if the request is already approved
            if (request.Status == RequestStatus.Approved)
            {
                throw new Exception("Approved leave requests cannot be deleted.");
            }

            // Mark the request and request leave as deleted
            request.IsDeleted = true;
            request.EmployeeIdLastDecider  = employeeIdDecider;
            if (request.RequestLeave != null)
            {
                request.RequestLeave.IsDeleted = true;
            }

            // Save the changes to the database
            await _dbContext.SaveChangesAsync();

            return new { message = "Leave request Deleted successfully." };
        }

        public async Task<bool> SendRequestLeaveToManagerFirebase(Guid requestId)
        {
            // Define the path specific to the manager
            string managerPath = "/managerNoti"; // Replace '/managerPath' with the actual path for the manager
                                                 // Call the SendLeaveRequestStatusToFirebase method with the manager path
            return await SendLeaveRequestStatusToFirebase(requestId, managerPath);
        }

        public async Task<bool> SendRequestLeaveToEmployeeFirebase(Guid requestId)
        {
            // Define the path specific to the employee
            string employeePath = "/employeeNoti"; // Replace '/employeePath' with the actual path for the employee
                                                   // Call the SendLeaveRequestStatusToFirebase method with the employee path
            return await SendLeaveRequestStatusToFirebase(requestId, employeePath);
        }

        public async Task<bool> SendLeaveRequestStatusToFirebase(Guid requestId, string path)
        {
            var request = await _dbContext.Requests
                .Include(r => r.RequestLeave)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return false; // Request not found
            }

            var manager = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeIdLastDecider);

            // Generate a reverse timestamp key for sorting in Firebase (newer entries first)
            long reverseTimestamp = DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks;

            var firebaseData = new
            {
                requestId = request.Id,
                employeeSenderId = request.EmployeeSendRequestId,
                employeeSenderName = request.EmployeeSendRequest?.FirstName + " " + request.EmployeeSendRequest?.LastName,
                employeeSenderNumber = request.EmployeeSendRequest?.EmployeeNumber,
                employeeDeciderId = request.EmployeeIdLastDecider,
                employeeDeciderName = manager?.FirstName + " " + manager?.LastName,
                employeeDeciderNumber = manager?.EmployeeNumber,
                leaveTypeId = request.RequestLeave.LeaveTypeId,
                status = request.Status.ToString(),
                reason = request.Reason,
                messageOfDecider = request.Message,
                submitedDate = request.SubmitedDate,
                fromDate = request.RequestLeave.FromDate,
                toDate = request.RequestLeave.ToDate,
                fromHour = (string)null,
                toHour = (string)null,
                actionDate = DateTime.Now,
                requestType = "Leave",
                isSeen = false
            };

            var json = JsonSerializer.Serialize(firebaseData);
            var httpClient = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Append the reverse timestamp key to the path to ensure ordering
            var result = await httpClient.PutAsync($"https://nextjs-course-f2de1-default-rtdb.firebaseio.com{path}/{reverseTimestamp}.json", content);

            return result.IsSuccessStatusCode;
        }

        public async Task<object> DeleteLeaveRequestByIdAsync(Guid requestId)
        {
            // Retrieve the request and its associated leave request by requestId
            var request = await _dbContext.Requests
                                          .Include(r => r.RequestLeave)
                                          .FirstOrDefaultAsync(r => r.Id == requestId);

            // Check if the request exists
            if (request == null)
            {
                throw new Exception("Request not found.");
            }

            // Check if the request is already deleted
            if (request.IsDeleted)
            {
                return "Request is already deleted.";
            }

            // Mark the request and its leave request as deleted
            request.IsDeleted = true;
            if (request.RequestLeave != null)
            {
                request.RequestLeave.IsDeleted = true;
            }

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            return "Request and its leave request have been successfully deleted.";
        }

        public async Task<object> GetCurrentYearLeaveInfo(Guid employeeId, Guid leaveTypeId)
        {
            var currentYear = DateTime.Now.Year;
            var employee = await _dbContext.Employees.Include(e => e.Department).ThenInclude(d => d.WorkTrackSetting).FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
            {
                throw new Exception("Employee not found.");
            }

            var workTrackSetting = employee.Department.WorkTrackSetting;
            if (workTrackSetting == null)
            {
                throw new Exception("WorkTrackSetting not found for the department.");
            }

            // Parse the MaxDateLeaves JSON string
            var maxDateLeaves = JsonSerializer.Deserialize<List<MaxDateLeavesDTO>>(workTrackSetting.MaxDateLeaves);
            if (workTrackSetting.MaxDateLeaves == "[]")
            {
                Dictionary<Guid, int> leaveTypeMaxDays = new Dictionary<Guid, int>
        {
            { Guid.Parse("790F290E-4CBD-11EE-BE56-0242AC120002"), 6 },
            { Guid.Parse("790F2378-4CBD-11EE-BE56-0242AC120002"), 8 },
            { Guid.Parse("790F277E-4CBD-11EE-BE56-0242AC120002"), 2 },
            { Guid.Parse("790F24A4-4CBD-11EE-BE56-0242AC120002"), 7 },
            { Guid.Parse("790F20A8-4CBD-11EE-BE56-0242AC120002"), 9 },
            { Guid.Parse("790F25C6-4CBD-11EE-BE56-0242AC120002"), 5 }
        };
                maxDateLeaves.Add(new MaxDateLeavesDTO
                {
                    Year = 2024,
                    LeaveTypeMaxDays = leaveTypeMaxDays
                });
            }
            var currentYearLeaveInfo = maxDateLeaves.FirstOrDefault(l => l.Year == currentYear)?.LeaveTypeMaxDays[leaveTypeId];

            var approvedLeaves = await _dbContext.Requests
                .Include(r => r.RequestLeave)
                .ThenInclude(rl => rl.WorkslotEmployees)
                .ThenInclude(we => we.Workslot)
                .Where(r => r.EmployeeSendRequestId == employeeId && r.Status == RequestStatus.Approved && r.RequestLeave.LeaveTypeId == leaveTypeId && r.RequestLeave.FromDate.Year == currentYear)
                .ToListAsync();

            var totalUsedDays = approvedLeaves.Sum(r => CountNumOfLeaveDate(getDateRangeOfRequestLeave(r.RequestLeave))); // Assuming inclusive dates

            var remainingDays = currentYearLeaveInfo.HasValue ? currentYearLeaveInfo.Value - totalUsedDays : 0;

            return new
            {
                StandardLeaveDays = currentYearLeaveInfo,
                CarryOverDays = 0, // Assuming no carryover for simplicity
                TotalUsedDays = totalUsedDays,
                RemainingDays = remainingDays
            };
        }

    }
}
