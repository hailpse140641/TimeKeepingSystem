
using BusinessObject.DTO;
using DataAccess.InterfaceRepository;
using DataAccess.InterfaceService;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace TimeKeepingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkSlotController : ControllerBase
    {
        private readonly IWorkslotService _service;
        private readonly IWorkslotRepository _repository;

        public WorkSlotController(IWorkslotService service, IWorkslotRepository repository)
        {
            _service = service;
            _repository = repository;
        }

        // GET: api/AttendanceStatus
        [HttpPost("generate-workslot-of-department-in-one-month")]
        public async Task<List<Workslot>> GenerateWorkSlotsForMonth(CreateWorkSlotRequest request)
        {
            return await _repository.GenerateWorkSlotsForMonth(request);
        }

        [HttpGet("get-workslot-of-department-in-one-month")]
        public async Task<List<object>> GetWorkSlotsForDepartment(Guid departmentId, string month)
        {
            return await _repository.GetWorkSlotsForDepartment(new CreateWorkSlotRequest() { departmentId = departmentId, month = month});
        }

        [HttpDelete("delete-duplicate-workslot")]
        public async Task<ActionResult<int>> RemoveDuplicateWorkSlots()
        {
            return await _repository.RemoveDuplicateWorkSlots();
        }

        [HttpGet("get-workslot-of-department-in-one-month-for-team")]
        public async Task<ActionResult<List<object>>> GetWorkSlotsForDepartmentOrEmployee(Guid? departmentId, string month, Guid? employeeId)
        {
            try
            {
                return Ok(await _repository.GetWorkSlotsForDepartmentOrEmployee(new CreateWorkSlotRequest() { departmentId = departmentId, month = month, employeeId= employeeId }));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-workslot-of-department-in-one-month-for-personal")]
        public async Task<ActionResult<List<object>>> GetWorkSlotsForPersonal(string month, Guid? employeeId)
        {
            try
            {
                return Ok(await _repository.GetWorkSlotsForPersonal(new CreateWorkSlotRequest() { month = month, employeeId = employeeId }));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

